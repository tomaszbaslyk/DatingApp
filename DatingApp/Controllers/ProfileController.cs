﻿using DatingApp.Models;
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
                Active = true
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
            var visitors = UnitOfWork.VisitorRepository.GetVisitorProfiles(model.Id);

            var viewModel = new ProfileIndexViewModel(model, visitors);

            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Index(int userId)
        {
            var model = UnitOfWork.ProfileRepository.GetProfile(userId);

            var visitorModel = new VisitorModel()
            {
                ProfileId = model.Id,
                VisitorId = UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId())
            };

            // om besökaren inte redan finns i besökarlistan och listan är mindre än 5
            if ((UnitOfWork.VisitorRepository.GetVisitorProfiles(model.Id).Count < 5) && 
                (!UnitOfWork.VisitorRepository.GetVisitorProfiles(model.Id).Contains(UnitOfWork.ProfileRepository.GetProfile(User.Identity.GetUserId()))))
            {
                UnitOfWork.VisitorRepository.AddVisitor(visitorModel);
            
            // om besökaren inte redan finns i besökarlistan, men listan är full
            } else if(!UnitOfWork.VisitorRepository.GetVisitorProfiles(model.Id).Contains(UnitOfWork.ProfileRepository.GetProfile(User.Identity.GetUserId())))
            {
                // den äldsta besökaren tas bort och den nya läggs till
                UnitOfWork.VisitorRepository.RemoveOldestVisitor();
                UnitOfWork.VisitorRepository.AddVisitor(visitorModel);
            }

            var viewModel = new ProfileIndexViewModel(model);

            UnitOfWork.Save();

            return View(viewModel);
        }

        [Authorize]
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
            string fileName = "";

            if (file != null)
            {
                string path = Path.Combine(Server.MapPath("~/Images"), Path.GetFileName(file.FileName));
                file.SaveAs(path);
                fileName = "~/Images/" + file.FileName;
            }
            string foreignKey = User.Identity.GetUserId();           

            var model = UnitOfWork.ProfileRepository.GetProfile(UnitOfWork.ProfileRepository.GetProfileId(foreignKey));
            model.Name = viewModel.Name;
            model.Age = viewModel.Age;
            model.Gender = viewModel.Gender;
            model.Biography = viewModel.Biography;

            if (!String.IsNullOrEmpty(fileName))
            {
                model.Image = fileName;
            }

            UnitOfWork.ProfileRepository.EditProfile(model);

            UnitOfWork.Save();
            return RedirectToAction("IndexMe", "Profile");
        }

        
        public ActionResult DisableAccount() {

            string foreignKey = User.Identity.GetUserId();

            var model = UnitOfWork.ProfileRepository.GetProfile(UnitOfWork.ProfileRepository.GetProfileId(foreignKey));
            model.Active = !model.Active;

            UnitOfWork.ProfileRepository.EditProfile(model);
            UnitOfWork.Save();

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
            var profiles = UnitOfWork.ProfileRepository.SearchProfiles(search);


            var currentId = User.Identity.GetUserId();
            int profileId = UnitOfWork.ProfileRepository.GetProfileId(currentId);

            var profilesViewModel = new ProfilesSearchViewModel();

            var contacts = UnitOfWork.ContactRepository.FindAllContacts(profileId);

            foreach (var profile in profiles)
            {
                if (profile.Id != profileId && profile.Active == true)
                {
                    bool isContact = false;

                    if (contacts.Contains(profile.Id))
                    {
                        isContact = true;
                    }

                    var profileViewModel = new ProfileSearchViewModel(profile, isContact);
                    profilesViewModel.Profiles.Add(profileViewModel);
                }
            }

            return View(profilesViewModel);
        }

        public ActionResult Download()
        {
            var profile = UnitOfWork.ProfileRepository.GetProfile(User.Identity.GetUserId());

            string path = Server.MapPath("~/ExportedUserData/" + profile.Id + ".xml");

            var downloadViewModel = new ProfileDownloadViewModel(profile);

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