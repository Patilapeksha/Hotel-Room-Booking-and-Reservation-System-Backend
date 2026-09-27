using HotelBooking.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var passwordHasher = new PasswordHasher<User>();

        // =========================
        // USERS
        // =========================

        var admin = await db.Users
            .FirstOrDefaultAsync(x => x.Email == "admin@hotels.test");

        if (admin == null)
        {
            admin = new User
            {
                Name = "Admin",
                Email = "admin@hotels.test",
                Role = "Admin",
                Status = "Active"
            };

            admin.PasswordHash =
                passwordHasher.HashPassword(admin, "Admin@123");

            db.Users.Add(admin);
        }

        var managerUser = await db.Users
            .FirstOrDefaultAsync(x => x.Email == "manager@hotels.test");

        if (managerUser == null)
        {
            managerUser = new User
            {
                Name = "Hotel Manager",
                Email = "manager@hotels.test",
                Role = "Hotel Manager",
                Status = "Active"
            };

            managerUser.PasswordHash =
                passwordHasher.HashPassword(
                    managerUser,
                    "Manager@123");

            db.Users.Add(managerUser);
        }

        var guest = await db.Users
            .FirstOrDefaultAsync(x => x.Email == "guest@hotels.test");

        if (guest == null)
        {
            guest = new User
            {
                Name = "Guest User",
                Email = "guest@hotels.test",
                Role = "Guest",
                Status = "Active"
            };

            guest.PasswordHash =
                passwordHasher.HashPassword(
                    guest,
                    "Guest@123");

            db.Users.Add(guest);
        }

        await db.SaveChangesAsync();

        // =========================
        // HOTEL MANAGER
        // =========================

        var manager = await db.HotelManagers
            .FirstOrDefaultAsync(x => x.UserId == managerUser.Id);

        if (manager == null)
        {
            manager = new HotelManager
            {
                UserId = managerUser.Id,
                ApprovalStatus = "Approved"
            };

            db.HotelManagers.Add(manager);
            await db.SaveChangesAsync();
        }

        // =========================
        // HOTELS
        // =========================

        if (!await db.Hotels.AnyAsync())
        {
            var hotels = new List<Hotel>
            {
                new Hotel
                {
                    ManagerId = manager.Id,
                    Name = "Grand Bengaluru Hotel",
                    Description = "Comfortable hotel in Bengaluru",
                    City = "Bengaluru",
                    Address = "MG Road, Bengaluru",
                    StarRating = 5,
                    Amenities = "WiFi, Pool, Gym, Restaurant"
                },

                new Hotel
                {
                    ManagerId = manager.Id,
                    Name = "City Comfort Hotel",
                    Description = "Modern hotel for business travellers",
                    City = "Mumbai",
                    Address = "Andheri, Mumbai",
                    StarRating = 4,
                    Amenities = "WiFi, Restaurant, Parking"
                },

                new Hotel
                {
                    ManagerId = manager.Id,
                    Name = "Green Valley Resort",
                    Description = "Relaxing resort stay",
                    City = "Goa",
                    Address = "Calangute, Goa",
                    StarRating = 4,
                    Amenities = "WiFi, Pool, Beach Access"
                }
            };

            db.Hotels.AddRange(hotels);
            await db.SaveChangesAsync();
        }

        // =========================
        // ROOM TYPES
        // =========================

        if (await db.RoomTypes.CountAsync() < 6)
        {
            var hotels = await db.Hotels
                .OrderBy(x => x.Id)
                .ToListAsync();

            var roomTypes = new List<RoomType>
            {
                new RoomType
                {
                    HotelId = hotels[0].Id,
                    TypeName = "Standard",
                    BaseRate = 2500,
                    MaxOccupancy = 2
                },

                new RoomType
                {
                    HotelId = hotels[0].Id,
                    TypeName = "Deluxe",
                    BaseRate = 4000,
                    MaxOccupancy = 3
                },

                new RoomType
                {
                    HotelId = hotels[1].Id,
                    TypeName = "Standard",
                    BaseRate = 2200,
                    MaxOccupancy = 2
                },

                new RoomType
                {
                    HotelId = hotels[1].Id,
                    TypeName = "Suite",
                    BaseRate = 5000,
                    MaxOccupancy = 4
                },

                new RoomType
                {
                    HotelId = hotels[2].Id,
                    TypeName = "Deluxe",
                    BaseRate = 3500,
                    MaxOccupancy = 3
                },

                new RoomType
                {
                    HotelId = hotels[2].Id,
                    TypeName = "Suite",
                    BaseRate = 6000,
                    MaxOccupancy = 5
                }
            };

            db.RoomTypes.AddRange(roomTypes);
            await db.SaveChangesAsync();
        }

        // =========================
        // ROOMS
        // =========================

        if (await db.Rooms.CountAsync() < 30)
        {
            var roomTypes = await db.RoomTypes
                .OrderBy(x => x.Id)
                .ToListAsync();

            var rooms = new List<Room>();

            foreach (var roomType in roomTypes)
            {
                for (int i = 1; i <= 5; i++)
                {
                    var roomNumber = $"{roomType.Id}{i:00}";

                    if (!await db.Rooms.AnyAsync(
                        x => x.RoomNumber == roomNumber))
                    {
                        rooms.Add(new Room
                        {
                            RoomTypeId = roomType.Id,
                            RoomNumber = roomNumber,
                            Status = "Active"
                        });
                    }
                }
            }

            db.Rooms.AddRange(rooms);
            await db.SaveChangesAsync();
        }

        // =========================
        // CANCELLATION POLICIES
        // =========================

        if (await db.CancellationPolicies.CountAsync() < 3)
        {
            var hotels = await db.Hotels
                .OrderBy(x => x.Id)
                .ToListAsync();

            db.CancellationPolicies.AddRange(
                new CancellationPolicy
                {
                    HotelId = hotels[0].Id,
                    DaysBeforeCheckIn = 3,
                    RefundPercentage = 100
                },
                new CancellationPolicy
                {
                    HotelId = hotels[1].Id,
                    DaysBeforeCheckIn = 2,
                    RefundPercentage = 75
                },
                new CancellationPolicy
                {
                    HotelId = hotels[2].Id,
                    DaysBeforeCheckIn = 5,
                    RefundPercentage = 100
                });

            await db.SaveChangesAsync();
        }

        // =========================
        // DYNAMIC RATE PLANS
        // =========================

        if (!await db.RatePlans.AnyAsync())
        {
            var roomTypes = await db.RoomTypes
                .OrderBy(x => x.Id)
                .ToListAsync();

            db.RatePlans.AddRange(
                new RatePlan
                {
                    RoomTypeId = roomTypes[0].Id,
                    DateFrom = new DateTime(2026, 10, 1),
                    DateTo = new DateTime(2026, 12, 31),
                    RateOverride = 2750,
                    RuleType = "Weekend"
                },

                new RatePlan
                {
                    RoomTypeId = roomTypes[1].Id,
                    DateFrom = new DateTime(2026, 10, 1),
                    DateTo = new DateTime(2026, 12, 31),
                    RateOverride = 4500,
                    RuleType = "Seasonal"
                },

                new RatePlan
                {
                    RoomTypeId = roomTypes[2].Id,
                    DateFrom = new DateTime(2026, 11, 1),
                    DateTo = new DateTime(2026, 12, 31),
                    RateOverride = 2500,
                    RuleType = "Seasonal"
                },

                new RatePlan
                {
                    RoomTypeId = roomTypes[3].Id,
                    DateFrom = new DateTime(2026, 10, 1),
                    DateTo = new DateTime(2026, 12, 31),
                    RateOverride = 5500,
                    RuleType = "Weekend"
                },

                new RatePlan
                {
                    RoomTypeId = roomTypes[4].Id,
                    DateFrom = new DateTime(2026, 10, 1),
                    DateTo = new DateTime(2026, 12, 31),
                    RateOverride = 3900,
                    RuleType = "Seasonal"
                },

                new RatePlan
                {
                    RoomTypeId = roomTypes[5].Id,
                    DateFrom = new DateTime(2026, 10, 1),
                    DateTo = new DateTime(2026, 12, 31),
                    RateOverride = 6500,
                    RuleType = "Weekend"
                });

            await db.SaveChangesAsync();
        }

        // =========================
        // SAMPLE RESERVATIONS
        // =========================

        if (await db.Reservations.CountAsync() < 15)
        {
            var hotels = await db.Hotels
                .OrderBy(x => x.Id)
                .ToListAsync();

            var rooms = await db.Rooms
                .OrderBy(x => x.Id)
                .ToListAsync();

            var reservations = new List<Reservation>();

            var bookingData = new[]
            {
                new { Room = 0, Hotel = 0, From = new DateTime(2026, 10, 10), To = new DateTime(2026, 10, 12), Status = "CheckedOut" },
                new { Room = 1, Hotel = 0, From = new DateTime(2026, 10, 10), To = new DateTime(2026, 10, 12), Status = "Confirmed" },
                new { Room = 2, Hotel = 0, From = new DateTime(2026, 10, 15), To = new DateTime(2026, 10, 18), Status = "Confirmed" },
                new { Room = 3, Hotel = 0, From = new DateTime(2026, 10, 15), To = new DateTime(2026, 10, 18), Status = "Cancelled" },
                new { Room = 4, Hotel = 0, From = new DateTime(2026, 11, 1), To = new DateTime(2026, 11, 4), Status = "Confirmed" },

                new { Room = 5, Hotel = 1, From = new DateTime(2026, 11, 2), To = new DateTime(2026, 11, 5), Status = "Confirmed" },
                new { Room = 6, Hotel = 1, From = new DateTime(2026, 11, 2), To = new DateTime(2026, 11, 5), Status = "Confirmed" },
                new { Room = 7, Hotel = 1, From = new DateTime(2026, 11, 10), To = new DateTime(2026, 11, 13), Status = "Confirmed" },
                new { Room = 8, Hotel = 1, From = new DateTime(2026, 11, 15), To = new DateTime(2026, 11, 18), Status = "Cancelled" },
                new { Room = 9, Hotel = 1, From = new DateTime(2026, 12, 1), To = new DateTime(2026, 12, 4), Status = "Confirmed" },

                new { Room = 10, Hotel = 2, From = new DateTime(2026, 12, 5), To = new DateTime(2026, 12, 8), Status = "Confirmed" },
                new { Room = 11, Hotel = 2, From = new DateTime(2026, 12, 5), To = new DateTime(2026, 12, 8), Status = "Confirmed" },
                new { Room = 12, Hotel = 2, From = new DateTime(2026, 12, 10), To = new DateTime(2026, 12, 13), Status = "Confirmed" },
                new { Room = 13, Hotel = 2, From = new DateTime(2026, 12, 15), To = new DateTime(2026, 12, 18), Status = "Cancelled" },
                new { Room = 14, Hotel = 2, From = new DateTime(2026, 12, 20), To = new DateTime(2026, 12, 23), Status = "Confirmed" }
            };

            foreach (var data in bookingData)
            {
                var room = rooms[data.Room];

                var nights =
                    (data.To - data.From).Days;

                var total =
                    room.RoomTypeId == 1 ? 2500 * nights :
                    room.RoomTypeId == 2 ? 4000 * nights :
                    room.RoomTypeId == 3 ? 2200 * nights :
                    room.RoomTypeId == 4 ? 5000 * nights :
                    room.RoomTypeId == 5 ? 3500 * nights :
                    6000 * nights;

                var reservation = new Reservation
                {
                    GuestId = guest.Id,
                    HotelId = hotels[data.Hotel].Id,
                    CheckInDate = data.From,
                    CheckOutDate = data.To,
                    TotalAmount = total,
                    Status = data.Status,
                    BookingReference =
                        "SEED-" +
                        Guid.NewGuid()
                            .ToString("N")
                            .Substring(0, 10)
                            .ToUpper()
                };

                reservations.Add(reservation);
            }

            db.Reservations.AddRange(reservations);
            await db.SaveChangesAsync();

            for (int i = 0; i < reservations.Count; i++)
            {
                var reservationRoom = new ReservationRoom
                {
                    ReservationId = reservations[i].Id,
                    RoomId = rooms[bookingData[i].Room].Id
                };

                db.ReservationRooms.Add(reservationRoom);

                if (reservations[i].Status == "Confirmed" ||
                    reservations[i].Status == "CheckedOut")
                {
                    db.Payments.Add(new Payment
                    {
                        ReservationId = reservations[i].Id,
                        Amount = reservations[i].TotalAmount,
                        GatewayReference =
                            "SEED-PAY-" +
                            Guid.NewGuid()
                                .ToString("N")
                                .Substring(0, 10)
                                .ToUpper(),
                        Status = "Paid"
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}