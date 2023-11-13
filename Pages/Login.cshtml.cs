using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LuceedConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LuceedTest_Web.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string AuthUsername { get; set; }
        [BindProperty]
        public string AuthPassword { get; set; }
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            if (!IsLoginFormValid())
            {
                return Page();
            }

            try
            {
                //Test API poziva da se provjeri točnost podataka
                Luceed luceed = new Luceed(AuthUsername, AuthPassword);
                var SalePoints = await luceed.GetSalePointsFromWarehouses();

                if(SalePoints.Count > 0)
                {
                    //Autentikacija uspješna
                    HttpContext.Session.SetString("Username", AuthUsername);
                    HttpContext.Session.SetString("Password", AuthPassword);
                    return Redirect("/Index");
                }
                else
                {
                    ModelState.AddModelError("All", "Authentication not successful.");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("All", "Authentication not successful.");
            }

            return Page();

        }

        private bool IsLoginFormValid()
        {
            if (string.IsNullOrEmpty(AuthUsername))
            {
                ModelState.AddModelError("AuthUsername", "Username can not be empty.");
            }
            if (string.IsNullOrEmpty(AuthPassword))
            {
                ModelState.AddModelError("AuthPassword", "Password can not be empty.");
            }

            return !(ModelState.ErrorCount > 0);
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("Password");
            return RedirectToPage("/Login");
        }
    }
}
