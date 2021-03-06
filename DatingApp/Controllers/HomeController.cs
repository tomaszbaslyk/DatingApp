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
            var viewModels = new ProfilesIndexViewModel();

            // Om ett konto precis inaktiverats, får viewbag:en en bekräftelse som skickas vidare till view
            ViewBag.Deactivated = TempData["Deactivated"];

            try
            {
                var profilesCount = UnitOfWork.ProfileRepository.CountProfiles();

                // Kontrollerar om det finns tillräckligt med profiler för att göra profilkorten på startsidan
                if (profilesCount > 3)
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        int profileId = UnitOfWork.ProfileRepository.GetProfileId(User.Identity.GetUserId());

                        foreach (var item in UnitOfWork.ProfileRepository.GetThreeNewestUsers(profileId))
                        {
                            var viewModel = new ProfileIndexViewModel(item);
                            viewModels.Profiles.Add(viewModel);
                        }
                    } else
                    {
                        foreach (var item in UnitOfWork.ProfileRepository.GetThreeNewestUsers())
                        {
                            var viewModel = new ProfileIndexViewModel(item);
                            viewModels.Profiles.Add(viewModel);
                        }
                    }

                    UnitOfWork.Dispose();

                    return View(viewModels);
                }
                else
                {
                    UnitOfWork.Dispose();
                    return View(viewModels);
                }
            }

            // Inga profiler kunde hämtas från databasen
            catch (InvalidOperationException)
            {
                return View(viewModels);
            }
        }
    }
}