using LineAccountLinkApp.Data;
using LineAccountLinkApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LineAccountLinkApp.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class ApiController : Controller
    {
        private ApplicationDbContext _dbContext;
        private UserManager<ApplicationUser> _userManager;

        public ApiController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        /// <summary>
        /// ユーザー情報を取得する
        /// </summary>
        [HttpGet("user/info")]
        public async Task<IActionResult> GetUserInfo([FromQuery]string nonce)
        {
            //指定したNonceを持つユーザーを検索
            var link = _dbContext.Set<LineLink>().FirstOrDefault(o => o.Nonce == nonce);
            if (link == null)
            {
                return Forbid("Invalid account link nonce.");
            }
            //ユーザーIDからユーザー情報を取得して返す
            var user = await _userManager.FindByIdAsync(link.UserId);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return new JsonResult(new { user.Id, user.UserName, user.Email });
        }

    }
}