﻿using DatingApp.Helpers;
using DatingApp.Models;
using DatingApp.Repositories;
using DatingApp.Services;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static DatingApp.Models.ProfileViewModels;

namespace DatingApp.Controllers
{
    public class ProfileController : Controller
    {
        private UnitOfWork UnitOfWork = new UnitOfWork();

        // GET: Profile
        public ActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Create(ProfileIndexViewModel profileModel)
        {
            var profile = new ProfileModel
            {
                Name = profileModel.Name,
                Age = profileModel.Age,
                Gender = profileModel.Gender,
                Biography = profileModel.Biography,
                Image = profileModel.Image,
                UserId = User.Identity.GetUserId(),
                Active = true,
                CSharp = profileModel.CSharp,
                JavaScript = profileModel.JavaScript,
                StackOverflow = profileModel.StackOverflow
            };

            UnitOfWork.ProfileRepository.AddProfile(profile);
            UnitOfWork.Save();

            return RedirectToAction("Index", "Home");

        }

        [Authorize]
        public ActionResult IndexMe()
        {
            string userId = User.Identity.GetUserId();
            var model = UnitOfWork.ProfileRepository.GetProfile(userId);

            var viewModel = new ProfileIndexViewModel(model);

            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Index(int userId)
        {
            var model = UnitOfWork.ProfileRepository.GetProfile(userId);

            // Checks if the visited profile is the same as the logged in user's
            if (UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId()) == model.Id)
            {
                return RedirectToAction("IndexMe");
            }

            var visitorModel = new VisitorModel()
            {
                ProfileId = model.Id,
                VisitorId = UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId())
            };

            var visitorModels = UnitOfWork.VisitorRepository.GetVisitorProfiles(model.Id);

            bool duplicate = false;

            // Checks if the current user already exists on the visitor list
            foreach (var visitor in visitorModels)
            {
                if (visitor.VisitorId == UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId()))
                {
                    duplicate = true;
                }
            }

            if ((visitorModels.Count < 5) && (!duplicate))
            {
                UnitOfWork.VisitorRepository.AddVisitor(visitorModel);
            }
            else if ((!duplicate) && (visitorModels.Count == 5))
            {
                UnitOfWork.VisitorRepository.RemoveOldestVisitor();
                UnitOfWork.VisitorRepository.AddVisitor(visitorModel);
            }
            else
            {
                UnitOfWork.VisitorRepository.RemoveVisitor(visitorModel.VisitorId);
                UnitOfWork.VisitorRepository.AddVisitor(visitorModel);
            }

            var viewModel = new ProfileIndexViewModel(model);

            UnitOfWork.Save();

            return View(viewModel);
        }

        [Authorize]
        //returnerar edit-viewn
        public ActionResult Edit()
        {
            string userId = User.Identity.GetUserId();
            var model = UnitOfWork.ProfileRepository.GetProfile(userId);
            var viewModel = new ProfileIndexViewModel(model);

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Edit(ProfileIndexViewModel viewModel, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                string foreignKey = User.Identity.GetUserId();

                var model = UnitOfWork.ProfileRepository.GetProfile(UnitOfWork.ProfileRepository.GetProfileId(foreignKey));
                model.Name = viewModel.Name;
                model.Age = viewModel.Age;
                model.Gender = viewModel.Gender;
                model.Biography = viewModel.Biography;
                model.CSharp = viewModel.CSharp;
                model.JavaScript = viewModel.JavaScript;
                model.StackOverflow = viewModel.StackOverflow;

                if (file != null)
                {
                    if (ImageHelper.IsValidExtension(file))
                    {
                        ImageHelper.Save(file);
                        string fileName = "~/Images/" + file.FileName;
                        model.Image = fileName;

                    } else
                    {
                        ViewBag.ErrorMessage = "Image must be a .png, .jpg or .jpeg";
                        return View(viewModel);
                    }
                }

                UnitOfWork.ProfileRepository.EditProfile(model);

                UnitOfWork.Save();
                return RedirectToAction("IndexMe", "Profile");
            }

            return View(viewModel);
        }


        public ActionResult DisableAccount()
        {

            string foreignKey = User.Identity.GetUserId();

            var model = UnitOfWork.ProfileRepository.GetProfile(UnitOfWork.ProfileRepository.GetProfileId(foreignKey));
            model.Active = !model.Active;

            UnitOfWork.ProfileRepository.EditProfile(model);
            UnitOfWork.Save();

            // Loggar ut användaren och skickar med tempdata om det
            HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            TempData["Deactivated"] = true;

            return RedirectToAction("Index", "Home");

        }

        [HttpPost]
        public ActionResult EnableAccount(string foreignKey)
        {
            var model = UnitOfWork.ProfileRepository.GetProfile(UnitOfWork.ProfileRepository.GetProfileId(foreignKey));
            model.Active = !model.Active;

            UnitOfWork.ProfileRepository.EditProfile(model);
            UnitOfWork.Save();

            TempData["Reactivated"] = true;

            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Search(string search)
        {
            if (search == null)
            {
                search = "";
            }

            var profiles = UnitOfWork.ProfileRepository.SearchProfiles(search);


            var currentId = User.Identity.GetUserId();
            int profileId = UnitOfWork.ProfileRepository.GetProfileId(currentId);

            var profilesSearchViewModel = new ProfilesSearchViewModel();

            var contacts = UnitOfWork.ContactRepository.FindAllContacts(profileId);

            foreach (var profile in profiles)
            {
                if (profile.Id != profileId && profile.Active == true)
                {
                    bool isContact = false;

                    // Kontrollerar om användaren i resultatet redan är kontakt med den inloggade användaren
                    if (contacts.Contains(profile.Id))
                    {
                        isContact = true;
                    }

                    var profileSearchViewModel = new ProfileSearchViewModel(profile, isContact, GetMatchPercentage(profile.Id));
                    profilesSearchViewModel.Profiles.Add(profileSearchViewModel);
                }
            }

            profilesSearchViewModel.Profiles = profilesSearchViewModel.Profiles.OrderByDescending(x => x.MatchPercentage).ToList();

            return View(profilesSearchViewModel);
        }

        [HttpGet]
        public int GetCurrentProfileId()
        {

            return UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId());

        }

        [HttpGet]
        /* Logiken för matchingen
           Generar en siffra mellan 0-100 (%) genom att jämföra ens olika utvecklarkunskpar samt ålder */
        public int GetMatchPercentage(int targetId)
        {

            int currentProfileId = UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId());

            var user1 = UnitOfWork.ProfileRepository.GetProfile(currentProfileId);
            var user2 = UnitOfWork.ProfileRepository.GetProfile(targetId);

            int csharpMatch, javaScriptMatch, stackOverflowMatch;
            int ageMatch = 0;

            if (user1.CSharp > user2.CSharp) { csharpMatch = user1.CSharp - user2.CSharp; }
            else { csharpMatch = user2.CSharp - user1.CSharp; }
            if (user1.JavaScript > user2.JavaScript) { javaScriptMatch = user1.JavaScript - user2.JavaScript; }
            else { javaScriptMatch = user2.JavaScript - user1.JavaScript; }
            if (user1.StackOverflow > user2.StackOverflow) { stackOverflowMatch = user1.StackOverflow - user2.StackOverflow; }
            else { stackOverflowMatch = user2.StackOverflow - user1.StackOverflow; }

            if (user1.Age > user2.Age) { 
                if (user1.Age - user2.Age < 10) { 
                    ageMatch = 10 - (user1.Age - user2.Age); } }
            else { 
                if (user2.Age - user1.Age < 10) { 
                    ageMatch = 10 - (user2.Age - user1.Age); } }

            return 100 - ((int)((csharpMatch + javaScriptMatch + stackOverflowMatch) * 3) + ageMatch);

        }

        public ActionResult Download()
        {
            var profile = UnitOfWork.ProfileRepository.GetProfile(User.Identity.GetUserId());

            string path = Server.MapPath("~/ExportedUserData/" + profile.Id + ".xml");

            var downloadViewModel = new ProfileDownloadViewModel(profile);

            // Serialiserar profilen
            XMLSerializer.Serialize<ProfileDownloadViewModel>(downloadViewModel, path);

            return RedirectToAction("IndexMe");
        }

        protected override void Dispose(bool disposing)
        {
            UnitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}