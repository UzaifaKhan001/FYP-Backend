﻿namespace FYP.Models
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }
    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
