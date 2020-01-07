﻿using DatingApp.Repositories;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static DatingApp.Models.ProfileViewModels;

namespace DatingApp.Controllers
{
    public class HomeController : Controller
    {
        private UnitOfWork UnitOfWork = new UnitOfWork();

        public ActionResult Index()
        {
            var profileId = UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId());

            var numbers = Enumerable.Range(1, UnitOfWork.ProfileRepository.CountProfiles()).OrderBy(n => n * n * (new Random()).Next());
            var distinctNumbers = numbers.Distinct().Take(3);

            while (distinctNumbers.Contains(profileId))
            {
                numbers = Enumerable.Range(1, UnitOfWork.ProfileRepository.CountProfiles()).OrderBy(n => n * n * (new Random()).Next());
                distinctNumbers = numbers.Distinct().Take(3);
            }

            var viewModels = new ProfilesIndexViewModel();

            foreach (int item in distinctNumbers)
            {
                var model = UnitOfWork.ProfileRepository.GetProfile(item);
                var viewModel = new ProfileIndexViewModel(model);
                viewModels.Profiles.Add(viewModel);
            }

            UnitOfWork.Dispose();

            return View(viewModels);
        }
    }
}