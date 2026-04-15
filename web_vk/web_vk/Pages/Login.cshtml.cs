using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace web_vk.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public IActionResult OnPost()
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Username == Username && x.Password == Password);

            if (user != null)
            {
                HttpContext.Session.SetString("user", user.Username);
                return RedirectToPage("/Dashboard");
            }

            ViewData["Error"] = "Sai tài khoản!";
            return Page();
        }
    }
}
