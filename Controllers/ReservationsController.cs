using System.Data;
using System.Security.Claims;
using HotelBooking.Api.Data;
using HotelBooking.Api.DTOs.Booking;
using HotelBooking.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Guest,Hotel Manager")]
public class ReservationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReservationsController(AppDbContext db)
    {
        _db = db;
    }

    // =========================================================
    // CREATE RESERVATION
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> CreateReservation(
        CreateReservationRequest request)
    {
        if (request.CheckInDate.Date >= request.CheckOutDate.Date)
        {
            return BadRequest(new
            {
                message = "Check-out date must be after check-in date."
            });
        }

        if (request.CheckInDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new
            {
                message = "Check-in date cannot be in the past."
            });
        }

        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            // Lock room row to prevent double booking
            var roomLock = await _db.Database
                .SqlQueryRaw<int>(
                    "SELECT Id AS Value FROM Rooms WITH (UPDLOCK, HOLDLOCK) WHERE Id = {0}",
                    request.RoomId)
                .FirstOrDefaultAsync();

            if (roomLock == 0)
            {
                await transaction.RollbackAsync();

                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            var room = await _db.Rooms
                .Include(r => r.RoomType)
                .ThenInclude(rt => rt.Hotel)
                .FirstOrDefaultAsync(r =>
                    r.Id == request.RoomId &&
                    r.Status == "Active");

            if (room == null)
            {
                await transaction.RollbackAsync();

                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            // Find user's valid room hold
            var hold = await _db.RoomHolds
                .FirstOrDefaultAsync(h =>
                    h.Id == request.HoldId &&
                    h.RoomId == request.RoomId &&
                    h.HeldByUserId == userId);

            if (hold == null)
            {
                await transaction.RollbackAsync();

                return NotFound(new
                {
                    message = "Hold not found."
                });
            }

            var now = DateTime.UtcNow;

            if (hold.HeldUntil <= now)
            {
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    message = "The room hold has expired."
                });
            }

            if (hold.DateFrom.Date != request.CheckInDate.Date ||
                hold.DateTo.Date != request.CheckOutDate.Date)
            {
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    message =
                        "Reservation dates do not match the room hold."
                });
            }

            // Check overlapping reservations
            var alreadyBooked =
                await _db.ReservationRooms.AnyAsync(rr =>
                    rr.RoomId == request.RoomId &&
                    (
                        rr.Reservation.Status == "Confirmed"
                        ||
                        (
                            rr.Reservation.Status == "PendingPayment" &&
                            rr.Reservation.ExpiresAt > now
                        )
                    )
                    &&
                    rr.Reservation.CheckInDate <
                        request.CheckOutDate.Date
                    &&
                    rr.Reservation.CheckOutDate >
                        request.CheckInDate.Date);

            if (alreadyBooked)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message =
                        "Room is already booked for these dates."
                });
            }

            // Calculate nights
            var nights =
                (request.CheckOutDate.Date -
                 request.CheckInDate.Date).Days;

            // Get applicable rate plans
            var ratePlans = await _db.RatePlans
                .Where(r =>
                    r.RoomTypeId == room.RoomTypeId &&
                    r.DateFrom < request.CheckOutDate.Date &&
                    r.DateTo > request.CheckInDate.Date)
                .ToListAsync();

            decimal totalAmount = 0;

            for (var date = request.CheckInDate.Date;
                 date < request.CheckOutDate.Date;
                 date = date.AddDays(1))
            {
                var ratePlan = ratePlans.FirstOrDefault(r =>
                    r.DateFrom.Date <= date &&
                    r.DateTo.Date > date);

                if (ratePlan != null)
                {
                    totalAmount += ratePlan.RateOverride;
                }
                else
                {
                    totalAmount += room.RoomType.BaseRate;
                }
            }

            // Generate booking reference
            var bookingReference =
                "HB-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 10)
                    .ToUpper();

            // Create reservation
            var reservation = new Reservation
            {
                GuestId = userId,
                HotelId = room.RoomType.HotelId,
                CheckInDate = request.CheckInDate.Date,
                CheckOutDate = request.CheckOutDate.Date,
                TotalAmount = totalAmount,
                Status = "PendingPayment",
                BookingReference = bookingReference,
                ExpiresAt = hold.HeldUntil
            };

            _db.Reservations.Add(reservation);

            await _db.SaveChangesAsync();

            // Add room
            var reservationRoom = new ReservationRoom
            {
                ReservationId = reservation.Id,
                RoomId = request.RoomId
            };

            _db.ReservationRooms.Add(reservationRoom);

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                message =
                    "Reservation created. Complete payment to confirm.",

                reservationId = reservation.Id,

                bookingReference =
                    reservation.BookingReference,

                roomId = request.RoomId,

                roomNumber = room.RoomNumber,

                hotel = room.RoomType.Hotel.Name,

                checkInDate =
                    reservation.CheckInDate,

                checkOutDate =
                    reservation.CheckOutDate,

                nights = nights,

                totalAmount =
                    reservation.TotalAmount,

                status =
                    reservation.Status
            });
        }
        catch
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new
            {
                message = "Unable to create reservation."
            });
        }
    }


    // =========================================================
    // GET RESERVATIONS
    //
    // Guest:
    //     Shows only their reservations
    //
    // Hotel Manager:
    //     Shows reservations for all hotels assigned
    //     to that manager
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> GetReservations()
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        var role =
            User.FindFirst(ClaimTypes.Role)?.Value;


        // =====================================================
        // GUEST
        // =====================================================

        if (role == "Guest")
        {
            var reservations =
                await _db.Reservations
                    .AsNoTracking()

                    .Where(r =>
                        r.GuestId == userId)

                    .Include(r => r.Hotel)

                    .Include(r => r.ReservationRooms)
                        .ThenInclude(rr => rr.Room)
                            .ThenInclude(room =>
                                room.RoomType)

                    .Include(r => r.Payments)

                    .OrderByDescending(r => r.Id)

                    .Select(r => new
                    {
                        reservationId =
                            r.Id,

                        bookingReference =
                            r.BookingReference,

                        hotel = new
                        {
                            id = r.Hotel.Id,

                            name =
                                r.Hotel.Name,

                            city =
                                r.Hotel.City
                        },

                        checkInDate =
                            r.CheckInDate,

                        checkOutDate =
                            r.CheckOutDate,

                        totalAmount =
                            r.TotalAmount,

                        status =
                            r.Status,

                        rooms =
                            r.ReservationRooms
                                .Select(rr => new
                                {
                                    roomId =
                                        rr.RoomId,

                                    roomNumber =
                                        rr.Room.RoomNumber,

                                    roomType =
                                        rr.Room.RoomType.TypeName
                                }),

                        paymentStatus =
                            r.Payments
                                .OrderByDescending(p => p.Id)
                                .Select(p => p.Status)
                                .FirstOrDefault()
                    })

                    .ToListAsync();

            return Ok(reservations);
        }


        // =====================================================
        // HOTEL MANAGER
        // =====================================================

        if (role == "Hotel Manager")
        {
            // IMPORTANT:
            // HotelManager has Hotels collection.
            // It does NOT have HotelId.

            var manager =
                await _db.HotelManagers
                    .Include(hm => hm.Hotels)
                    .FirstOrDefaultAsync(hm =>
                        hm.UserId == userId);

            if (manager == null)
            {
                return NotFound(new
                {
                    message =
                        "Hotel manager assignment not found."
                });
            }

            if (!manager.Hotels.Any())
            {
                return NotFound(new
                {
                    message =
                        "No hotels are assigned to this manager."
                });
            }

            // Get all hotel IDs assigned to this manager
            var managerHotelIds =
                manager.Hotels
                    .Select(h => h.Id)
                    .ToList();


            var reservations =
                await _db.Reservations
                    .AsNoTracking()

                    .Where(r =>
                        managerHotelIds.Contains(
                            r.HotelId))

                    .Include(r => r.Guest)

                    .Include(r => r.Hotel)

                    .Include(r => r.ReservationRooms)
                        .ThenInclude(rr => rr.Room)
                            .ThenInclude(room =>
                                room.RoomType)

                    .Include(r => r.Payments)

                    .OrderByDescending(r => r.Id)

                    .Select(r => new
                    {
                        reservationId =
                            r.Id,

                        bookingReference =
                            r.BookingReference,

                        guestName =
                            r.Guest.Name,

                        guestEmail =
                            r.Guest.Email,

                        hotel = new
                        {
                            id =
                                r.Hotel.Id,

                            name =
                                r.Hotel.Name,

                            city =
                                r.Hotel.City
                        },

                        checkInDate =
                            r.CheckInDate,

                        checkOutDate =
                            r.CheckOutDate,

                        totalAmount =
                            r.TotalAmount,

                        status =
                            r.Status,

                        rooms =
                            r.ReservationRooms
                                .Select(rr => new
                                {
                                    roomId =
                                        rr.RoomId,

                                    roomNumber =
                                        rr.Room.RoomNumber,

                                    roomType =
                                        rr.Room.RoomType.TypeName
                                }),

                        paymentStatus =
                            r.Payments
                                .OrderByDescending(p => p.Id)
                                .Select(p => p.Status)
                                .FirstOrDefault()
                    })

                    .ToListAsync();

            return Ok(reservations);
        }


        return Forbid();
    }


    // =========================================================
    // GET SINGLE RESERVATION
    // =========================================================

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReservation(int id)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }

        var role =
            User.FindFirst(ClaimTypes.Role)?.Value;


        var reservation =
            await _db.Reservations
                .AsNoTracking()

                .Include(r => r.Hotel)

                .Include(r => r.Guest)

                .Include(r => r.ReservationRooms)
                    .ThenInclude(rr => rr.Room)
                        .ThenInclude(room =>
                            room.RoomType)

                .Include(r => r.Payments)

                .FirstOrDefaultAsync(r =>
                    r.Id == id);


        if (reservation == null)
        {
            return NotFound(new
            {
                message =
                    "Reservation not found."
            });
        }


        // =====================================================
        // GUEST
        // =====================================================

        if (role == "Guest")
        {
            if (reservation.GuestId != userId)
            {
                return Forbid();
            }
        }


        // =====================================================
        // HOTEL MANAGER
        // =====================================================

        else if (role == "Hotel Manager")
        {
            var manager =
                await _db.HotelManagers
                    .Include(hm => hm.Hotels)
                    .FirstOrDefaultAsync(hm =>
                        hm.UserId == userId);

            if (manager == null)
            {
                return NotFound(new
                {
                    message =
                        "Hotel manager assignment not found."
                });
            }

            if (!manager.Hotels.Any())
            {
                return NotFound(new
                {
                    message =
                        "No hotels are assigned to this manager."
                });
            }

            var managerHotelIds =
                manager.Hotels
                    .Select(h => h.Id)
                    .ToList();

            // IMPORTANT:
            // No manager.HotelId here.
            if (!managerHotelIds.Contains(
                    reservation.HotelId))
            {
                return Forbid();
            }
        }

        else
        {
            return Forbid();
        }


        return Ok(new
        {
            reservationId =
                reservation.Id,

            bookingReference =
                reservation.BookingReference,

            guest = new
            {
                id =
                    reservation.Guest.Id,

                name =
                    reservation.Guest.Name,

                email =
                    reservation.Guest.Email
            },

            hotel = new
            {
                id =
                    reservation.Hotel.Id,

                name =
                    reservation.Hotel.Name,

                city =
                    reservation.Hotel.City,

                address =
                    reservation.Hotel.Address
            },

            checkInDate =
                reservation.CheckInDate,

            checkOutDate =
                reservation.CheckOutDate,

            totalAmount =
                reservation.TotalAmount,

            status =
                reservation.Status,

            rooms =
                reservation.ReservationRooms
                    .Select(rr => new
                    {
                        roomId =
                            rr.RoomId,

                        roomNumber =
                            rr.Room.RoomNumber,

                        roomType =
                            rr.Room.RoomType.TypeName
                    }),

            payments =
                reservation.Payments
                    .Select(p => new
                    {
                        paymentId =
                            p.Id,

                        amount =
                            p.Amount,

                        status =
                            p.Status,

                        gatewayReference =
                            p.GatewayReference,

                        createdAt =
                            p.CreatedAt
                    })
        });
    }


    // =========================================================
    // CANCEL RESERVATION
    // =========================================================

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> CancelReservation(
        int id)
    {
        var userIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user token."
            });
        }


        var reservation =
            await _db.Reservations
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.GuestId == userId);


        if (reservation == null)
        {
            return NotFound(new
            {
                message =
                    "Reservation not found."
            });
        }


        if (reservation.Status == "Cancelled")
        {
            return BadRequest(new
            {
                message =
                    "Reservation is already cancelled."
            });
        }


        if (reservation.Status == "Completed")
        {
            return BadRequest(new
            {
                message =
                    "Completed reservation cannot be cancelled."
            });
        }


        reservation.Status =
            "Cancelled";


        await _db.SaveChangesAsync();


        return Ok(new
        {
            message =
                "Reservation cancelled successfully.",

            reservationId =
                reservation.Id,

            bookingReference =
                reservation.BookingReference,

            status =
                reservation.Status
        });
    }
}

