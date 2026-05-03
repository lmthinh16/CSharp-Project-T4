using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using web_vk.Models;

namespace web_vk.Pages
{
    public class ToursModel : PageModel
    {
        private readonly AppDbContext _context;
        public ToursModel(AppDbContext context) => _context = context;

        // Kh?i t?o = new() ?? tránh l?i null khi constructor ch?y xong
        public List<Tour> Tours { get; set; } = new();
        public List<Restaurant> AllRestaurants { get; set; } = new();

        public async Task OnGetAsync()
        {
            Tours = await _context.Tours.OrderByDescending(t => t.CreatedAt).ToListAsync();
            AllRestaurants = await _context.Restaurants.OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync(string TourTitle, string Description, int[] SelectedRestaurantIds)
        {
            if (SelectedRestaurantIds == null || SelectedRestaurantIds.Length == 0) return Page();

            var newTour = new Tour
            {
                Title = TourTitle,
                Description = Description,
                CreatedAt = DateTime.Now,
                TotalEstimatedTime = "60-90 phút" // Giá tr? t?m th?i
            };

            _context.Tours.Add(newTour);
            await _context.SaveChangesAsync();

            // L?u vào b?ng trung gian TourDetails
            for (int i = 0; i < SelectedRestaurantIds.Length; i++)
            {
                _context.TourDetails.Add(new TourDetail
                {
                    TourId = newTour.Id,
                    RestaurantId = SelectedRestaurantIds[i],
                    OrderIndex = i + 1 // Th? t? trong tour
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}