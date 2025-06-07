using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TennisCourtBookingSystem.Filters;
using TennisCourtBookingSystem.Models;

namespace TennisCourtBookingSystem.Controllers
{
    [NoCache]
    public class AccountController : Controller
    {
        TennisDbContext db = new TennisDbContext();

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                if (db.Users.Any(x => x.Email == user.Email))
                {
                    ViewBag.Message = "Email already registered.";
                    return View(user);
                }

                user.Role = "User";
                db.Users.Add(user);
                db.SaveChanges();
                ViewBag.Message = "Registration successful. Please login.";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Message = "User not found. Please register first.";
                return View();
            }

            if (user.Password != password)
            {
                ViewBag.Message = "Incorrect password. Please try again.";
                return View();
            }

            Session["UserId"] = user.Id;
            Session["UserName"] = user.Name;
            Session["Role"] = user.Role;

            return user.Role == "Admin" ? RedirectToAction("AdminDashboard") : RedirectToAction("UserDashboard");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        public ActionResult UserDashboard()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login");

            int userId = Convert.ToInt32(Session["UserId"]);
            var bookings = db.Bookings
                             .Where(b => b.UserId == userId)
                             .OrderByDescending(b => b.BookingDate)
                             .ToList();

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View(bookings);
        }

        public ActionResult AdminDashboard(string searchTerm = "")
        {
            List<User> users;
            List<Booking> bookings;
            string userMessage = "";

            if (string.IsNullOrEmpty(searchTerm))
            {
                users = db.Users.ToList();
                bookings = db.Bookings.ToList();
            }
            else
            {
                var user = db.Users.FirstOrDefault(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm));
                if (user != null)
                {
                    users = new List<User> { user };
                    bookings = db.Bookings.Where(b => b.UserId == user.Id).ToList();

                    if (bookings.Count == 0)
                        userMessage = "This user has not booked any slot.";
                }
                else
                {
                    users = new List<User>();
                    bookings = new List<Booking>();
                    userMessage = "User not found.";
                }
            }

            var viewModel = new AdminDashboardViewModel
            {
                Users = users,
                Bookings = bookings,
                SearchTerm = searchTerm
            };

            ViewBag.UserMessage = userMessage;
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult DeleteUser(int id)
        {
            var user = db.Users.Find(id);
            if (user != null)
            {
                var userBookings = db.Bookings.Where(b => b.UserId == id).ToList();
                db.Bookings.RemoveRange(userBookings);
                db.Users.Remove(user);
                db.SaveChanges();
            }
            return RedirectToAction("AdminDashboard");
        }

        [HttpPost]
        
        public ActionResult DeleteBooking(int id)
        {
            if (Session["UserId"] == null || Session["Role"] == null)
                return RedirectToAction("Login");

            string role = Session["Role"].ToString();
            int currentUserId = Convert.ToInt32(Session["UserId"]);

            var booking = db.Bookings.FirstOrDefault(b => b.Id == id);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
            }
            else if (role == "Admin" || booking.UserId == currentUserId)
            {
                db.Bookings.Remove(booking);
                db.SaveChanges();
                TempData["Success"] = "Booking deleted successfully.";
            }
            else
            {
                TempData["Error"] = "You are not authorized to delete this booking.";
            }

            return role == "Admin" ? RedirectToAction("AdminDashboard") : RedirectToAction("UserDashboard");
        }

    }
}
