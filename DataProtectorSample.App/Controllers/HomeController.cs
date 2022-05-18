using DataProtectorSample.App.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataProtectorSample.App.Controllers
{
    public class HomeController : Controller
    {
        
        private readonly IDataProtector _dataProtector;

        public HomeController(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtector = dataProtectionProvider.CreateProtector("MySuperKeyword");
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> UserList(bool withtimelimit=false)
        {
            var userList = await GetUserFromFile();
            
            if (withtimelimit) {
                var timeProtector = _dataProtector.ToTimeLimitedDataProtector();
                userList.ForEach(x => x.EncrptedId = timeProtector.Protect(x.Id.ToString(), TimeSpan.FromSeconds(10)));
                ViewBag.Title = "links will be invalid after 10 seconds";
                ViewBag.Timeless = false;
            } else
            {
                userList.ForEach(x => x.EncrptedId = _dataProtector.Protect(x.Id.ToString()));
                ViewBag.Title = "links live forever";
                ViewBag.Timeless = true;
            }
            

            return View(userList);
        }


        public async Task<IActionResult> ShowUser(string id,bool timeless) {
            if ( string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            User user = null;
            var users = await GetUserFromFile();
            if (timeless)
            {
                user = users.Where(I => I.Id == int.Parse(_dataProtector.Unprotect(id))).FirstOrDefault();
            }
            else
            {
                var protector = _dataProtector.ToTimeLimitedDataProtector();
                int userId = int.Parse(protector.Unprotect(id));
                user = users.Where(I => I.Id == userId).FirstOrDefault();
            }



            if (user == null)
            {
                return NotFound();
            }

            return View(user);

            
        }




        private static async Task<List<User>> GetUserFromFile() {

            var userFile = Path.Combine(Directory.GetCurrentDirectory(), $"Data\\user.json");
            var JSON = await System.IO.File.ReadAllTextAsync(userFile, Encoding.UTF8);
            var users = JsonConvert.DeserializeObject<List<User>>(JSON);
            return users.OrderBy(I => I.Id).ToList();


        }
    }
}
