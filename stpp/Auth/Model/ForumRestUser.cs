using Microsoft.AspNetCore.Identity;

namespace stpp.Auth.Model
{
    public class ForumRestUser : IdentityUser
    {
        public bool ForceRelogin { get; set; }
    }
}
