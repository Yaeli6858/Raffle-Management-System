using Microsoft.EntityFrameworkCore;
using server.Data;
using server.DTOs.Donors;
using server.Models;
using server.Models.Enums;
using server.Services.Interfaces;
using AutoMapper;

namespace server.Services
{
    public class DonorService : IDonorService
    {
        private readonly AppDbContext _context;
        private readonly IAuthService authService;
        private readonly IGiftService giftService;
        private readonly ILogger<DonorService> _logger;
        private readonly IMapper _mapper;

        public DonorService(AppDbContext context, IAuthService authService, IGiftService giftService, ILogger<DonorService> logger, IMapper mapper)
        {
            _context = context;
            this.authService = authService;
            this.giftService = giftService;
            _logger = logger;
            _mapper = mapper;
        }
        ///(Role = Donor), עם אפשרות סינון לפי חיפוש ולפי עיר.
        // -------- פעולות אדמין --------
        public async Task<IEnumerable<DonorListItemDto>> GetDonorsAsync(string? search, string? city)
        {
            var q = _context.Users
                .Where(u => u.Role == RoleEnum.Donor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                q = q.Where(u =>
                    u.Name.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.Phone.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                city = city.Trim();
                q = q.Where(u => u.City.Contains(city));
            }

            return await _mapper
                .ProjectTo<DonorListItemDto>(q.OrderBy(u => u.Name))
                .Take(100).ToListAsync();

        }

        public async Task<IEnumerable<DonorWithGiftsDto>> GetDonorsWithGiftsAsync()
        {
            var donors = await _context.Users
                .Where(u => u.Role == RoleEnum.Donor)
                .Take(100).ToListAsync();

            var gifts = await giftService.GetAllGiftsAsync(PriceSort.None, null, null);

            return donors.Select(d =>
            {
                var dto = _mapper.Map<DonorWithGiftsDto>(d);

                dto.Gifts = gifts
                    .Where(g => g.DonorId == d.Id)
                    .ToList();

                return dto;
            });


        }

        /// לשנות תפקיד (Role) למשתמש מסוים — פעולה של אדמין.
        public async Task SetUserRoleAsync(int userId, RoleEnum role)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            user.Role = role;
            await _context.SaveChangesAsync();
        }

        // דשבורד לתורם 
        /// להחזיר “דשבורד לתורם” — נתונים מסכמים לתורם ספציפי: מתנות שלו, כמות כרטיסים שנמכרו, כמה קונים ייחודיים לכל מתנה, והאם יש זכייה.
        public async Task<DonorDashboardResponseDto> GetDonorDashboardAsync(int donorId)
        {
            var donor = await _context.Users.FirstOrDefaultAsync(u => u.Id == donorId);
            if (donor == null) throw new KeyNotFoundException("Donor user not found");

            // 1) מתנות של התורם
            var gifts = await _context.Gifts
                .Where(g => g.DonorId == donorId)
                .Select(g => new { g.Id, g.Description })
                .Take(100).ToListAsync();

            var giftIds = gifts.Select(g => g.Id).ToList();

            // 2)  רכישות Completed 
            var purchaseStats = await _context.Purchases
                .Where(p => giftIds.Contains(p.GiftId) && p.Status == Status.Completed)
                .GroupBy(p => p.GiftId)
                .Select(grp => new
                {
                    GiftId = grp.Key,
                    TicketsSold = grp.Sum(x => x.Qty),
                    UniqueBuyers = grp.Select(x => x.UserId).Distinct().Count()
                })
                .Take(100).ToListAsync();

            // 3) זכיות לפי GiftId
            var winningGiftIds = await _context.Winnings
                .Where(w => giftIds.Contains(w.GiftId))
                .Select(w => w.GiftId)
                .Distinct()
                .Take(100).ToListAsync();

            int Tickets(int giftId) =>
                purchaseStats.FirstOrDefault(x => x.GiftId == giftId)?.TicketsSold ?? 0;

            int Buyers(int giftId) =>
                purchaseStats.FirstOrDefault(x => x.GiftId == giftId)?.UniqueBuyers ?? 0;

            return new DonorDashboardResponseDto
            {
                DonorId = donor.Id,
                DonorName = donor.Name,

                TotalGifts = gifts.Count,
                TotalTicketsSold = purchaseStats.Sum(x => x.TicketsSold),
                TotalUniqueBuyers = purchaseStats.Sum(x => x.UniqueBuyers),
                Gifts = gifts.Select(g => new DonorGiftStatsDto
                {
                    GiftId = g.Id,
                    Description = g.Description,
                    TicketsSold = Tickets(g.Id),
                    UniqueBuyers = Buyers(g.Id),
                    HasWinning = winningGiftIds.Contains(g.Id)
                }).ToList()
            };
        }

        public async Task<DonorListItemDto?> GetDonorDetailsAsync(int userId)
        {
            return await _mapper
                .ProjectTo<DonorListItemDto>(
                    _context.Users.Where(u => u.Id == userId && u.Role == RoleEnum.Donor)
                )
                .FirstOrDefaultAsync();

        }

        public async Task<addDonorDto> AddDonorAsync(addDonorDto donorDto)
        {
            var existingUser = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == donorDto.Email);

            if (existingUser != null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var newDonor = new UserModel
            {
                Name = donorDto.Name,
                Email = donorDto.Email,
                Phone = donorDto.Phone,
                City = donorDto.City,
                Address = donorDto.Address,
                Role = RoleEnum.Donor,
                IsActive = true
            };

            //  Hash סיסמה
            newDonor.Password = authService.HashPassword(donorDto.Password);

            _context.Users.Add(newDonor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added new donor: {@DonorDto}", donorDto);


            return donorDto;
        }
    }
}