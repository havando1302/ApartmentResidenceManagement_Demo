using System;
using System.Linq;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApartmentResidenceManagement.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Đảm bảo database đã được tạo và chạy migration
        context.Database.Migrate();

        // Chỉ seed dữ liệu khi bảng UserAccounts chưa có dữ liệu (lần đầu khởi động)
        if (context.UserAccounts.Any())
        {
            return; // Đã có dữ liệu, không cần seed lại
        }

        // 3. Tạo dữ liệu mẫu (Seed Data)

        
        // 3.1. Căn hộ mẫu (Apartment)
        var apartments = new[]
        {
            // Giữ nguyên 5 căn đầu để các quan hệ lịch sử cư trú cũ không bị lệch index
            new Apartment { ApartmentNumber = "101", Floor = 1, Area = 50.5, Status = ApartmentStatus.Empty }, // index 0
            new Apartment { ApartmentNumber = "102", Floor = 1, Area = 50.5, Status = ApartmentStatus.Empty }, // index 1
            new Apartment { ApartmentNumber = "201", Floor = 2, Area = 75.0, Status = ApartmentStatus.Occupied }, // index 2
            new Apartment { ApartmentNumber = "202", Floor = 2, Area = 75.0, Status = ApartmentStatus.Occupied }, // index 3
            new Apartment { ApartmentNumber = "301", Floor = 3, Area = 110.0, Status = ApartmentStatus.UnderMaintenance }, // index 4

            // Tầng 1
            new Apartment { ApartmentNumber = "103", Floor = 1, Area = 58.0, Status = ApartmentStatus.Occupied },
            // Tầng 2
            new Apartment { ApartmentNumber = "203", Floor = 2, Area = 86.5, Status = ApartmentStatus.Empty },
            // Tầng 3
            new Apartment { ApartmentNumber = "302", Floor = 3, Area = 58.5, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "303", Floor = 3, Area = 65.0, Status = ApartmentStatus.Occupied },
            // Tầng 4
            new Apartment { ApartmentNumber = "401", Floor = 4, Area = 42.0, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "402", Floor = 4, Area = 55.5, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "403", Floor = 4, Area = 92.0, Status = ApartmentStatus.Empty },
            // Tầng 5
            new Apartment { ApartmentNumber = "501", Floor = 5, Area = 48.5, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "502", Floor = 5, Area = 68.0, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "503", Floor = 5, Area = 105.5, Status = ApartmentStatus.UnderMaintenance },
            // Tầng 6
            new Apartment { ApartmentNumber = "601", Floor = 6, Area = 35.5, Status = ApartmentStatus.Empty },
            new Apartment { ApartmentNumber = "602", Floor = 6, Area = 72.0, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "603", Floor = 6, Area = 86.5, Status = ApartmentStatus.Occupied },
            // Tầng 7
            new Apartment { ApartmentNumber = "701", Floor = 7, Area = 45.0, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "702", Floor = 7, Area = 65.5, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "703", Floor = 7, Area = 92.0, Status = ApartmentStatus.Occupied },
            // Tầng 8
            new Apartment { ApartmentNumber = "801", Floor = 8, Area = 42.5, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "802", Floor = 8, Area = 58.0, Status = ApartmentStatus.Empty },
            new Apartment { ApartmentNumber = "803", Floor = 8, Area = 105.5, Status = ApartmentStatus.Occupied },
            // Tầng 9
            new Apartment { ApartmentNumber = "901", Floor = 9, Area = 48.0, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "902", Floor = 9, Area = 72.0, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "903", Floor = 9, Area = 86.5, Status = ApartmentStatus.UnderMaintenance },
            // Tầng 10
            new Apartment { ApartmentNumber = "1001", Floor = 10, Area = 58.5, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "1002", Floor = 10, Area = 92.0, Status = ApartmentStatus.Occupied },
            new Apartment { ApartmentNumber = "1003", Floor = 10, Area = 105.5, Status = ApartmentStatus.Empty }
        };
        context.Apartments.AddRange(apartments);
        context.SaveChanges();

        // 3.2. Cư dân mẫu (Resident)
        var residents = new[]
        {
            // Cư dân gốc (Phải giữ nguyên Id/thông tin để tài khoản hoạt động)
            new Resident { FullName = "Nguyễn Văn A", DateOfBirth = new DateTime(1980, 1, 1), Gender = GenderType.Male, IdentityCard = "123456789012", PhoneNumber = "0901234567", HomeTown = "Hà Nội" }, // index 0
            new Resident { FullName = "Trần Thị B", DateOfBirth = new DateTime(1985, 2, 2), Gender = GenderType.Female, IdentityCard = "123456789013", PhoneNumber = "0901234568", HomeTown = "Nam Định" }, // index 1
            new Resident { FullName = "Nguyễn Văn C", DateOfBirth = new DateTime(1990, 3, 3), Gender = GenderType.Male, IdentityCard = "123456789014", PhoneNumber = "0901234569", HomeTown = "Thanh Hóa" }, // index 2
            new Resident { FullName = "Lê Văn D", DateOfBirth = new DateTime(1995, 4, 4), Gender = GenderType.Male, IdentityCard = "123456789015", PhoneNumber = "0901234570", HomeTown = "Hải Phòng" }, // index 3

            // Cư dân bổ sung để đạt quy mô chung cư thực tế (51 cư dân tiếp theo)
            new Resident { FullName = "Lê Thị E", DateOfBirth = new DateTime(1987, 5, 12), Gender = GenderType.Female, IdentityCard = "001087012345", PhoneNumber = "0912345601", HomeTown = "Hà Nội" }, // index 4
            new Resident { FullName = "Phạm Văn F", DateOfBirth = new DateTime(1982, 8, 20), Gender = GenderType.Male, IdentityCard = "001082012346", PhoneNumber = "0912345602", HomeTown = "Hòa Bình" }, // index 5
            new Resident { FullName = "Ngô Văn G", DateOfBirth = new DateTime(1978, 11, 3), Gender = GenderType.Male, IdentityCard = "001078012347", PhoneNumber = "0912345603", HomeTown = "Vĩnh Phúc" }, // index 6
            new Resident { FullName = "Ngô Thị H", DateOfBirth = new DateTime(1984, 4, 15), Gender = GenderType.Female, IdentityCard = "001084012348", PhoneNumber = "0912345604", HomeTown = "Vĩnh Phúc" }, // index 7
            new Resident { FullName = "Lý Văn I", DateOfBirth = new DateTime(1993, 2, 28), Gender = GenderType.Male, IdentityCard = "001093012349", PhoneNumber = "0912345605", HomeTown = "Lạng Sơn" }, // index 8
            new Resident { FullName = "Vũ Văn K", DateOfBirth = new DateTime(1989, 7, 9), Gender = GenderType.Male, IdentityCard = "001089012350", PhoneNumber = "0912345606", HomeTown = "Thái Bình" }, // index 9
            new Resident { FullName = "Vũ Thị L", DateOfBirth = new DateTime(1994, 9, 14), Gender = GenderType.Female, IdentityCard = "001094012351", PhoneNumber = "0912345607", HomeTown = "Thái Bình" }, // index 10
            new Resident { FullName = "Bùi Văn M", DateOfBirth = new DateTime(1975, 12, 5), Gender = GenderType.Male, IdentityCard = "001075012352", PhoneNumber = "0912345608", HomeTown = "Quảng Ninh" }, // index 11
            new Resident { FullName = "Hoàng Văn N", DateOfBirth = new DateTime(1981, 3, 22), Gender = GenderType.Male, IdentityCard = "001081012353", PhoneNumber = "0912345609", HomeTown = "Nghệ An" }, // index 12
            new Resident { FullName = "Hoàng Thị O", DateOfBirth = new DateTime(1985, 6, 17), Gender = GenderType.Female, IdentityCard = "001085012354", PhoneNumber = "0912345610", HomeTown = "Nghệ An" }, // index 13
            new Resident { FullName = "Hoàng Văn P", DateOfBirth = new DateTime(2012, 10, 8), Gender = GenderType.Male, IdentityCard = "001212012355", PhoneNumber = "0912345611", HomeTown = "Hà Nội" }, // index 14
            new Resident { FullName = "Đỗ Văn Q", DateOfBirth = new DateTime(1991, 1, 30), Gender = GenderType.Male, IdentityCard = "001091012356", PhoneNumber = "0912345612", HomeTown = "Hải Dương" }, // index 15
            new Resident { FullName = "Dương Văn R", DateOfBirth = new DateTime(1976, 5, 25), Gender = GenderType.Male, IdentityCard = "001076012357", PhoneNumber = "0912345613", HomeTown = "Bắc Ninh" }, // index 16
            new Resident { FullName = "Dương Thị S", DateOfBirth = new DateTime(1982, 8, 14), Gender = GenderType.Female, IdentityCard = "001082012358", PhoneNumber = "0912345614", HomeTown = "Bắc Ninh" }, // index 17
            new Resident { FullName = "Dương Văn T", DateOfBirth = new DateTime(2008, 11, 2), Gender = GenderType.Male, IdentityCard = "001208012359", PhoneNumber = "0912345615", HomeTown = "Bắc Ninh" }, // index 18
            new Resident { FullName = "Phan Văn U", DateOfBirth = new DateTime(1983, 10, 11), Gender = GenderType.Male, IdentityCard = "001083012360", PhoneNumber = "0912345616", HomeTown = "Hà Tĩnh" }, // index 19
            new Resident { FullName = "Phan Thị V", DateOfBirth = new DateTime(1988, 12, 20), Gender = GenderType.Female, IdentityCard = "001088012361", PhoneNumber = "0912345617", HomeTown = "Hà Tĩnh" }, // index 20
            new Resident { FullName = "Phan Văn W", DateOfBirth = new DateTime(2015, 3, 5), Gender = GenderType.Male, IdentityCard = "001215012362", PhoneNumber = "0912345618", HomeTown = "Hà Nội" }, // index 21
            new Resident { FullName = "Đặng Văn X", DateOfBirth = new DateTime(1979, 9, 18), Gender = GenderType.Male, IdentityCard = "001079012363", PhoneNumber = "0912345619", HomeTown = "Thừa Thiên Huế" }, // index 22
            new Resident { FullName = "Đặng Thị Y", DateOfBirth = new DateTime(1983, 11, 24), Gender = GenderType.Female, IdentityCard = "001083012364", PhoneNumber = "0912345620", HomeTown = "Thừa Thiên Huế" }, // index 23
            new Resident { FullName = "Đặng Văn Z", DateOfBirth = new DateTime(2010, 4, 12), Gender = GenderType.Male, IdentityCard = "001210012365", PhoneNumber = "0912345621", HomeTown = "Hà Nội" }, // index 24
            new Resident { FullName = "Trịnh Văn A2", DateOfBirth = new DateTime(1988, 7, 7), Gender = GenderType.Male, IdentityCard = "001088012366", PhoneNumber = "0912345622", HomeTown = "Quảng Bình" }, // index 25
            new Resident { FullName = "Trịnh Thị B2", DateOfBirth = new DateTime(1992, 1, 15), Gender = GenderType.Female, IdentityCard = "001092012367", PhoneNumber = "0912345623", HomeTown = "Quảng Bình" }, // index 26
            new Resident { FullName = "Trịnh Văn C2", DateOfBirth = new DateTime(2020, 6, 9), Gender = GenderType.Male, IdentityCard = "001220012368", PhoneNumber = "0912345624", HomeTown = "Hà Nội" }, // index 27
            new Resident { FullName = "Mai Văn D2", DateOfBirth = new DateTime(1974, 2, 14), Gender = GenderType.Male, IdentityCard = "001074012369", PhoneNumber = "0912345625", HomeTown = "Thanh Hóa" }, // index 28
            new Resident { FullName = "Mai Thị E2", DateOfBirth = new DateTime(1978, 6, 23), Gender = GenderType.Female, IdentityCard = "001078012370", PhoneNumber = "0912345626", HomeTown = "Thanh Hóa" }, // index 29
            new Resident { FullName = "Mai Văn F2", DateOfBirth = new DateTime(2003, 10, 1), Gender = GenderType.Male, IdentityCard = "001203012371", PhoneNumber = "0912345627", HomeTown = "Thanh Hóa" }, // index 30
            new Resident { FullName = "Mai Thị G2", DateOfBirth = new DateTime(2006, 12, 11), Gender = GenderType.Female, IdentityCard = "001206012372", PhoneNumber = "0912345628", HomeTown = "Thanh Hóa" }, // index 31
            new Resident { FullName = "Hồ Văn H2", DateOfBirth = new DateTime(1982, 3, 30), Gender = GenderType.Male, IdentityCard = "001082012373", PhoneNumber = "0912345629", HomeTown = "Hải Phòng" }, // index 32
            new Resident { FullName = "Hồ Thị I2", DateOfBirth = new DateTime(1985, 7, 24), Gender = GenderType.Female, IdentityCard = "001085012374", PhoneNumber = "0912345630", HomeTown = "Hải Phòng" }, // index 33
            new Resident { FullName = "Hồ Văn K2", DateOfBirth = new DateTime(2011, 9, 15), Gender = GenderType.Male, IdentityCard = "001211012375", PhoneNumber = "0912345631", HomeTown = "Hà Nội" }, // index 34
            new Resident { FullName = "Hồ Thị L2", DateOfBirth = new DateTime(2014, 5, 20), Gender = GenderType.Female, IdentityCard = "001214012376", PhoneNumber = "0912345632", HomeTown = "Hà Nội" }, // index 35
            new Resident { FullName = "Đoàn Văn M2", DateOfBirth = new DateTime(1977, 8, 9), Gender = GenderType.Male, IdentityCard = "001077012377", PhoneNumber = "0912345633", HomeTown = "Nam Định" }, // index 36
            new Resident { FullName = "Đoàn Thị N2", DateOfBirth = new DateTime(1981, 12, 28), Gender = GenderType.Female, IdentityCard = "001081012378", PhoneNumber = "0912345634", HomeTown = "Nam Định" }, // index 37
            new Resident { FullName = "Đoàn Văn O2", DateOfBirth = new DateTime(2005, 4, 18), Gender = GenderType.Male, IdentityCard = "001205012379", PhoneNumber = "0912345635", HomeTown = "Nam Định" }, // index 38
            new Resident { FullName = "Đoàn Thị P2", DateOfBirth = new DateTime(2009, 1, 22), Gender = GenderType.Female, IdentityCard = "001209012380", PhoneNumber = "0912345636", HomeTown = "Nam Định" }, // index 39
            new Resident { FullName = "Lâm Văn Q2", DateOfBirth = new DateTime(1980, 11, 15), Gender = GenderType.Male, IdentityCard = "001080012381", PhoneNumber = "0912345637", HomeTown = "Bình Định" }, // index 40
            new Resident { FullName = "Lâm Thị R2", DateOfBirth = new DateTime(1984, 5, 3), Gender = GenderType.Female, IdentityCard = "001084012382", PhoneNumber = "0912345638", HomeTown = "Bình Định" }, // index 41
            new Resident { FullName = "Lâm Văn S2", DateOfBirth = new DateTime(2010, 8, 12), Gender = GenderType.Male, IdentityCard = "001210012383", PhoneNumber = "0912345639", HomeTown = "Hà Nội" }, // index 42
            new Resident { FullName = "Lâm Thị T2", DateOfBirth = new DateTime(2013, 10, 25), Gender = GenderType.Female, IdentityCard = "001213012384", PhoneNumber = "0912345640", HomeTown = "Hà Nội" }, // index 43
            new Resident { FullName = "Tô Văn U2", DateOfBirth = new DateTime(1975, 4, 1), Gender = GenderType.Male, IdentityCard = "001075012385", PhoneNumber = "0912345641", HomeTown = "Thái Nguyên" }, // index 44
            new Resident { FullName = "Tô Thị V2", DateOfBirth = new DateTime(1979, 9, 12), Gender = GenderType.Female, IdentityCard = "001079012386", PhoneNumber = "0912345642", HomeTown = "Thái Nguyên" }, // index 45
            new Resident { FullName = "Tô Văn W2", DateOfBirth = new DateTime(2001, 12, 30), Gender = GenderType.Male, IdentityCard = "001201012387", PhoneNumber = "0912345643", HomeTown = "Thái Nguyên" }, // index 46
            new Resident { FullName = "Tô Thị X2", DateOfBirth = new DateTime(2004, 3, 15), Gender = GenderType.Female, IdentityCard = "001204012388", PhoneNumber = "0912345644", HomeTown = "Thái Nguyên" }, // index 47
            new Resident { FullName = "Tô Văn Y2", DateOfBirth = new DateTime(2009, 6, 20), Gender = GenderType.Male, IdentityCard = "001209012389", PhoneNumber = "0912345645", HomeTown = "Hà Nội" }, // index 48
            new Resident { FullName = "Diệp Văn Z2", DateOfBirth = new DateTime(1973, 10, 5), Gender = GenderType.Male, IdentityCard = "001073012390", PhoneNumber = "0912345646", HomeTown = "Phú Thọ" }, // index 49
            new Resident { FullName = "Diệp Thị A3", DateOfBirth = new DateTime(1977, 1, 18), Gender = GenderType.Female, IdentityCard = "001077012391", PhoneNumber = "0912345647", HomeTown = "Phú Thọ" }, // index 50
            new Resident { FullName = "Diệp Văn B3", DateOfBirth = new DateTime(2000, 5, 22), Gender = GenderType.Male, IdentityCard = "001200012392", PhoneNumber = "0912345648", HomeTown = "Phú Thọ" }, // index 51
            new Resident { FullName = "Diệp Thị C3", DateOfBirth = new DateTime(2003, 8, 14), Gender = GenderType.Female, IdentityCard = "001203012393", PhoneNumber = "0912345649", HomeTown = "Phú Thọ" }, // index 52
            new Resident { FullName = "Diệp Văn D3", DateOfBirth = new DateTime(2007, 11, 9), Gender = GenderType.Male, IdentityCard = "001207012394", PhoneNumber = "0912345650", HomeTown = "Hà Nội" }, // index 53
            new Resident { FullName = "Trần Văn E3", DateOfBirth = new DateTime(1996, 2, 28), Gender = GenderType.Male, IdentityCard = "001096012395", PhoneNumber = "0912345651", HomeTown = "Hà Nam" } // index 54
        };
        context.Residents.AddRange(residents);
        context.SaveChanges();

        // 3.3. Tài khoản mẫu (UserAccount)
        // Băm mật khẩu bằng BCrypt
        string adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        string residentPasswordHash = BCrypt.Net.BCrypt.HashPassword("resident123");

        var userAccounts = new[]
        {
            new UserAccount 
            { 
                Username = "admin", 
                PasswordHash = adminPasswordHash, 
                Role = UserRole.Admin, 
                ResidentId = null, 
                IsActive = true 
            },
            new UserAccount 
            { 
                Username = "resident_a", 
                PasswordHash = residentPasswordHash, 
                Role = UserRole.Resident, 
                ResidentId = residents[0].Id, // Nguyễn Văn A
                IsActive = true 
            },
            new UserAccount 
            { 
                Username = "resident_c", 
                PasswordHash = residentPasswordHash, 
                Role = UserRole.Resident, 
                ResidentId = residents[2].Id, // Nguyễn Văn C
                IsActive = true 
            }
        };
        context.UserAccounts.AddRange(userAccounts);
        context.SaveChanges();

        // 3.4. Lịch sử cư trú mẫu (ResidenceHistory)
        var residenceHistories = new[]
        {
            // Căn 103 (Index 5) - Có 2 người
            new ResidenceHistory { ApartmentId = apartments[5].Id, ResidentId = residents[1].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2026, 1, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[5].Id, ResidentId = residents[4].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2026, 1, 1), IsActive = true },

            // Căn 201 (Index 2) - Có 1 người và 1 lịch sử chuyển đi
            new ResidenceHistory { ApartmentId = apartments[2].Id, ResidentId = residents[0].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 1, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[2].Id, ResidentId = residents[1].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 12, 31), IsActive = false }, // Trần Thị B từng ở đây

            // Căn 202 (Index 3) - Có 2 người
            new ResidenceHistory { ApartmentId = apartments[3].Id, ResidentId = residents[2].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 2, 2), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[3].Id, ResidentId = residents[3].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 2, 2), IsActive = true },

            // Căn 302 (Index 7) - Có 1 người và 1 lịch sử thuê cũ
            new ResidenceHistory { ApartmentId = apartments[7].Id, ResidentId = residents[5].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2026, 1, 15), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[7].Id, ResidentId = residents[54].Id, RelationshipType = RelationshipType.Tenant, StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 12, 31), IsActive = false }, // Trần Văn E3 thuê cũ

            // Căn 303 (Index 8) - Có 2 người
            new ResidenceHistory { ApartmentId = apartments[8].Id, ResidentId = residents[6].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 6, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[8].Id, ResidentId = residents[7].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 6, 1), IsActive = true },

            // Căn 401 (Index 9) - Có 1 người
            new ResidenceHistory { ApartmentId = apartments[9].Id, ResidentId = residents[8].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 3, 10), IsActive = true },

            // Căn 402 (Index 10) - Có 2 người
            new ResidenceHistory { ApartmentId = apartments[10].Id, ResidentId = residents[9].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 4, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[10].Id, ResidentId = residents[10].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 4, 1), IsActive = true },

            // Căn 501 (Index 12) - Có 1 người
            new ResidenceHistory { ApartmentId = apartments[12].Id, ResidentId = residents[11].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 5, 20), IsActive = true },

            // Căn 502 (Index 13) - Có 3 người
            new ResidenceHistory { ApartmentId = apartments[13].Id, ResidentId = residents[12].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 8, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[13].Id, ResidentId = residents[13].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 8, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[13].Id, ResidentId = residents[14].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 8, 1), IsActive = true },

            // Căn 602 (Index 16) - Có 1 người
            new ResidenceHistory { ApartmentId = apartments[16].Id, ResidentId = residents[15].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 11, 1), IsActive = true },

            // Căn 603 (Index 17) - Có 3 người
            new ResidenceHistory { ApartmentId = apartments[17].Id, ResidentId = residents[16].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 9, 15), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[17].Id, ResidentId = residents[17].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 9, 15), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[17].Id, ResidentId = residents[18].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 9, 15), IsActive = true },

            // Căn 701 (Index 18) - Có 3 người
            new ResidenceHistory { ApartmentId = apartments[18].Id, ResidentId = residents[19].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 7, 10), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[18].Id, ResidentId = residents[20].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 7, 10), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[18].Id, ResidentId = residents[21].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 7, 10), IsActive = true },

            // Căn 702 (Index 19) - Có 3 người
            new ResidenceHistory { ApartmentId = apartments[19].Id, ResidentId = residents[22].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 12, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[19].Id, ResidentId = residents[23].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 12, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[19].Id, ResidentId = residents[24].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 12, 1), IsActive = true },

            // Căn 703 (Index 20) - Có 3 người
            new ResidenceHistory { ApartmentId = apartments[20].Id, ResidentId = residents[25].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 10, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[20].Id, ResidentId = residents[26].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 10, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[20].Id, ResidentId = residents[27].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 10, 1), IsActive = true },

            // Căn 801 (Index 21) - Có 4 người
            new ResidenceHistory { ApartmentId = apartments[21].Id, ResidentId = residents[28].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 2, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[21].Id, ResidentId = residents[29].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 2, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[21].Id, ResidentId = residents[30].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 2, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[21].Id, ResidentId = residents[31].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 2, 1), IsActive = true },

            // Căn 803 (Index 23) - Có 4 người
            new ResidenceHistory { ApartmentId = apartments[23].Id, ResidentId = residents[32].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 3, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[23].Id, ResidentId = residents[33].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 3, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[23].Id, ResidentId = residents[34].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 3, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[23].Id, ResidentId = residents[35].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 3, 1), IsActive = true },

            // Căn 901 (Index 24) - Có 4 người
            new ResidenceHistory { ApartmentId = apartments[24].Id, ResidentId = residents[36].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 4, 15), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[24].Id, ResidentId = residents[37].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 4, 15), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[24].Id, ResidentId = residents[38].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 4, 15), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[24].Id, ResidentId = residents[39].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 4, 15), IsActive = true },

            // Căn 902 (Index 25) - Có 4 người
            new ResidenceHistory { ApartmentId = apartments[25].Id, ResidentId = residents[40].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 5, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[25].Id, ResidentId = residents[41].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 5, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[25].Id, ResidentId = residents[42].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 5, 1), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[25].Id, ResidentId = residents[43].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 5, 1), IsActive = true },

            // Căn 1001 (Index 27) - Có 5 người
            new ResidenceHistory { ApartmentId = apartments[27].Id, ResidentId = residents[44].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 1, 10), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[27].Id, ResidentId = residents[45].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 1, 10), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[27].Id, ResidentId = residents[46].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 1, 10), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[27].Id, ResidentId = residents[47].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 1, 10), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[27].Id, ResidentId = residents[48].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 1, 10), IsActive = true },

            // Căn 1002 (Index 28) - Có 5 người
            new ResidenceHistory { ApartmentId = apartments[28].Id, ResidentId = residents[49].Id, RelationshipType = RelationshipType.Owner, StartDate = new DateTime(2025, 6, 20), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[28].Id, ResidentId = residents[50].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 6, 20), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[28].Id, ResidentId = residents[51].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 6, 20), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[28].Id, ResidentId = residents[52].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 6, 20), IsActive = true },
            new ResidenceHistory { ApartmentId = apartments[28].Id, ResidentId = residents[53].Id, RelationshipType = RelationshipType.FamilyMember, StartDate = new DateTime(2025, 6, 20), IsActive = true },

            // Lịch sử thuê phòng 101 cũ (đã chuyển đi)
            new ResidenceHistory { ApartmentId = apartments[0].Id, ResidentId = residents[2].Id, RelationshipType = RelationshipType.Tenant, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), IsActive = false }
        };
        context.ResidenceHistories.AddRange(residenceHistories);
        context.SaveChanges();

        // 3.5. Phương tiện mẫu (Vehicle)
        // Phân bổ hợp lý theo quy mô hộ và căn hộ
        var vehicles = new[]
        {
            // ── Căn 103 (residents[1]=Trần Thị B, residents[4]=Lê Thị E) — 2 người ──
            new Vehicle { LicensePlate = "29A1-11201", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[1].Id },
            new Vehicle { LicensePlate = "29A1-11202", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[4].Id },

            // ── Căn 201 (residents[0]=Nguyễn Văn A) — 1 người ──
            new Vehicle { LicensePlate = "29A1-12345", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[0].Id },
            new Vehicle { LicensePlate = "30A-09876",  VehicleType = VehicleType.Car,  Brand = "Toyota",  OwnerId = residents[0].Id },

            // ── Căn 202 (residents[2]=Nguyễn Văn C, residents[3]=Lê Văn D) — 2 người ──
            new Vehicle { LicensePlate = "30B-99999",  VehicleType = VehicleType.Car,  Brand = "Toyota",  OwnerId = residents[2].Id },
            new Vehicle { LicensePlate = "29A1-12302", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[3].Id },

            // ── Căn 302 (residents[5]=Phạm Văn F) — 1 người ──
            new Vehicle { LicensePlate = "29A1-30201", VehicleType = VehicleType.Moto, Brand = "Suzuki",  OwnerId = residents[5].Id },

            // ── Căn 303 (residents[6]=Ngô Văn G, residents[7]=Ngô Thị H) — 2 người ──
            new Vehicle { LicensePlate = "29A1-30301", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[6].Id },
            new Vehicle { LicensePlate = "30F-30302",  VehicleType = VehicleType.Car,  Brand = "Mazda",   OwnerId = residents[6].Id },

            // ── Căn 401 (residents[8]=Lý Văn I) — 1 người ──
            new Vehicle { LicensePlate = "29A1-40101", VehicleType = VehicleType.Moto, Brand = "Piaggio", OwnerId = residents[8].Id },

            // ── Căn 402 (residents[9]=Vũ Văn K, residents[10]=Vũ Thị L) — 2 người ──
            new Vehicle { LicensePlate = "29A1-40201", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[9].Id },
            new Vehicle { LicensePlate = "29A1-40202", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[10].Id },
            new Vehicle { LicensePlate = "30G-40203",  VehicleType = VehicleType.Car,  Brand = "Honda",   OwnerId = residents[9].Id },

            // ── Căn 501 (residents[11]=Bùi Văn M) — 1 người ──
            new Vehicle { LicensePlate = "29A1-50101", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[11].Id },

            // ── Căn 502 (residents[12]=Hoàng Văn N, residents[13]=Hoàng Thị O, residents[14]=Hoàng Văn P) — 3 người ──
            new Vehicle { LicensePlate = "29A1-50201", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[12].Id },
            new Vehicle { LicensePlate = "30H-50202",  VehicleType = VehicleType.Car,  Brand = "Kia",     OwnerId = residents[12].Id },
            new Vehicle { LicensePlate = "29A1-50203", VehicleType = VehicleType.Bicycle, Brand = "Giant", OwnerId = residents[14].Id },

            // ── Căn 602 (residents[15]=Đỗ Văn Q) — 1 người ──
            new Vehicle { LicensePlate = "29A1-60201", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[15].Id },
            new Vehicle { LicensePlate = "30K-60202",  VehicleType = VehicleType.Car,  Brand = "Ford",    OwnerId = residents[15].Id },

            // ── Căn 603 (residents[16]=Dương Văn R, residents[17]=Dương Thị S, residents[18]=Dương Văn T) — 3 người ──
            new Vehicle { LicensePlate = "29A1-60301", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[16].Id },
            new Vehicle { LicensePlate = "30L-60302",  VehicleType = VehicleType.Car,  Brand = "Hyundai", OwnerId = residents[16].Id },
            new Vehicle { LicensePlate = "29A1-60303", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[17].Id },

            // ── Căn 701 (residents[19]=Phan Văn U, residents[20]=Phan Thị V, residents[21]=Phan Văn W) — 3 người ──
            new Vehicle { LicensePlate = "29A1-70101", VehicleType = VehicleType.Moto, Brand = "Suzuki",  OwnerId = residents[19].Id },
            new Vehicle { LicensePlate = "30M-70102",  VehicleType = VehicleType.Car,  Brand = "Mazda",   OwnerId = residents[19].Id },
            new Vehicle { LicensePlate = "29A1-70103", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[20].Id },

            // ── Căn 702 (residents[22]=Đặng Văn X, residents[23]=Đặng Thị Y, residents[24]=Đặng Văn Z) — 3 người ──
            new Vehicle { LicensePlate = "29A1-70201", VehicleType = VehicleType.Moto, Brand = "Piaggio", OwnerId = residents[22].Id },
            new Vehicle { LicensePlate = "29A1-70202", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[23].Id },
            new Vehicle { LicensePlate = "29A1-70203", VehicleType = VehicleType.Bicycle, Brand = "Trek", OwnerId = residents[24].Id },

            // ── Căn 703 (residents[25]=Trịnh Văn A2, residents[26]=Trịnh Thị B2, residents[27]=Trịnh Văn C2) — 3 người ──
            new Vehicle { LicensePlate = "29A1-70301", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[25].Id },
            new Vehicle { LicensePlate = "30N-70302",  VehicleType = VehicleType.Car,  Brand = "Vinfast", OwnerId = residents[25].Id },
            new Vehicle { LicensePlate = "29A1-70303", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[26].Id },

            // ── Căn 801 (residents[28..31] = Mai Văn D2, Mai Thị E2, Mai Văn F2, Mai Thị G2) — 4 người ──
            new Vehicle { LicensePlate = "29A1-80101", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[28].Id },
            new Vehicle { LicensePlate = "30P-80102",  VehicleType = VehicleType.Car,  Brand = "Toyota",  OwnerId = residents[28].Id },
            new Vehicle { LicensePlate = "29A1-80103", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[29].Id },
            new Vehicle { LicensePlate = "29A1-80104", VehicleType = VehicleType.Bicycle, Brand = "Giant", OwnerId = residents[30].Id },

            // ── Căn 803 (residents[32..35] = Hồ Văn H2, Hồ Thị I2, Hồ Văn K2, Hồ Thị L2) — 4 người ──
            new Vehicle { LicensePlate = "29A1-80301", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[32].Id },
            new Vehicle { LicensePlate = "30Q-80302",  VehicleType = VehicleType.Car,  Brand = "Kia",     OwnerId = residents[32].Id },
            new Vehicle { LicensePlate = "29A1-80303", VehicleType = VehicleType.Moto, Brand = "Piaggio", OwnerId = residents[33].Id },
            new Vehicle { LicensePlate = "29A1-80304", VehicleType = VehicleType.Moto, Brand = "Suzuki",  OwnerId = residents[34].Id },

            // ── Căn 901 (residents[36..39] = Đoàn Văn M2, Đoàn Thị N2, Đoàn Văn O2, Đoàn Thị P2) — 4 người ──
            new Vehicle { LicensePlate = "29A1-90101", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[36].Id },
            new Vehicle { LicensePlate = "30R-90102",  VehicleType = VehicleType.Car,  Brand = "Hyundai", OwnerId = residents[36].Id },
            new Vehicle { LicensePlate = "29A1-90103", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[37].Id },
            new Vehicle { LicensePlate = "29A1-90104", VehicleType = VehicleType.Bicycle, Brand = "Trek", OwnerId = residents[39].Id },

            // ── Căn 902 (residents[40..43] = Lâm Văn Q2, Lâm Thị R2, Lâm Văn S2, Lâm Thị T2) — 4 người ──
            new Vehicle { LicensePlate = "29A1-90201", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[40].Id },
            new Vehicle { LicensePlate = "30S-90202",  VehicleType = VehicleType.Car,  Brand = "Mazda",   OwnerId = residents[40].Id },
            new Vehicle { LicensePlate = "29A1-90203", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[41].Id },
            new Vehicle { LicensePlate = "29A1-90204", VehicleType = VehicleType.Moto, Brand = "Piaggio", OwnerId = residents[42].Id },

            // ── Căn 1001 (residents[44..48] = Tô Văn U2, Tô Thị V2, Tô Văn W2, Tô Thị X2, Tô Văn Y2) — 5 người ──
            new Vehicle { LicensePlate = "29A1-10001", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[44].Id },
            new Vehicle { LicensePlate = "30T-10002",  VehicleType = VehicleType.Car,  Brand = "Vinfast", OwnerId = residents[44].Id },
            new Vehicle { LicensePlate = "29A1-10003", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[45].Id },
            new Vehicle { LicensePlate = "29A1-10004", VehicleType = VehicleType.Moto, Brand = "Suzuki",  OwnerId = residents[46].Id },
            new Vehicle { LicensePlate = "29A1-10005", VehicleType = VehicleType.Bicycle, Brand = "Giant", OwnerId = residents[47].Id },

            // ── Căn 1002 (residents[49..53] = Diệp Văn Z2, Diệp Thị A3, Diệp Văn B3, Diệp Thị C3, Diệp Văn D3) — 5 người ──
            new Vehicle { LicensePlate = "29A1-10011", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[49].Id },
            new Vehicle { LicensePlate = "30U-10012",  VehicleType = VehicleType.Car,  Brand = "Toyota",  OwnerId = residents[49].Id },
            new Vehicle { LicensePlate = "29A1-10013", VehicleType = VehicleType.Moto, Brand = "Yamaha",  OwnerId = residents[50].Id },
            new Vehicle { LicensePlate = "29A1-10014", VehicleType = VehicleType.Moto, Brand = "Honda",   OwnerId = residents[51].Id },
            new Vehicle { LicensePlate = "29A1-10015", VehicleType = VehicleType.Bicycle, Brand = "Trek", OwnerId = residents[52].Id },
        };
        context.Vehicles.AddRange(vehicles);
        context.SaveChanges();

    }
}
