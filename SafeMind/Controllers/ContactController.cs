using SafeMind.Data.Models;
using Microsoft.AspNetCore.Mvc;
using SafeMind.Data;
using SafeMind.Models;

namespace SafeMind.Controllers
{
    public class ContactController : Controller
    {
        private readonly SafeMindDbContext _context;

        public ContactController(SafeMindDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var message = new ContactMessage
            {
                FullName = model.FullName,
                Email = model.Email,
                Subject = model.Subject,
                Message = model.Message,
                SubmittedOn = DateTimeOffset.UtcNow
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            TempData["ContactSuccess"] = "Your message has been sent successfully. We'll get back to you soon!";
            return RedirectToAction("Index");
        }
    }
}
