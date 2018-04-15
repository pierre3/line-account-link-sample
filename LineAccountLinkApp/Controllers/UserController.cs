﻿using LineAccountLinkApp.Data;
using LineAccountLinkApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LineAccountLinkApp.Controllers
{
    [Produces("application/json")]
    [Route("api/User")]
    public class UserController : Controller
    {
        private ApplicationDbContext _dbContext;
        private UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet("Info/{nonce}")]
        public async Task<IActionResult> GetInfo(string nonce)
        {
            
            var link = await _dbContext.FindAsync<LineLink>(nonce);
            if (link == null)
            {
                return BadRequest("Invalid account link nonce");
            }
            var user = await _userManager.FindByIdAsync(link.UserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return new JsonResult(new { user.Id, user.UserName, user.Email });
        }
    }
}