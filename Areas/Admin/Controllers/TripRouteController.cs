﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripGuide.Data;
using TripGuide.Models;
using TripGuide.Repositories;
using TripGuide.Repository;
using TripGuide.Utility;

namespace TripGuide.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = (StaticDetail.Role_Admin + "," + StaticDetail.Role_Moderator))]
    public class TripRouteController : Controller
    {
        private readonly TripGuideDbContext context;
        private readonly ITripRouteRepository tripRouteRepository;
        private readonly ITouristObjectRepository touristObjectRepository;

        [BindProperty]
        public TripRoute TripRoute { get; set; }
        public List<Guid> TouristObjectIds { get; set; }

        public TripRouteController(TripGuideDbContext context, ITripRouteRepository tripRouteRepository, ITouristObjectRepository touristObjectRepository)
        {
            this.context = context;
            this.tripRouteRepository = tripRouteRepository;
            this.touristObjectRepository = touristObjectRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            var touristObjects = touristObjectRepository.GetAll().ToList();
            ViewBag.TouristObjects = touristObjects;
            return View();
        }

        [HttpPost]
        public IActionResult Add(TripRoute tripRoute, List<Guid> TouristObjectIds)
        {
            if (TouristObjectIds != null && TouristObjectIds.Count >= 2)
            {
                tripRoute = tripRouteRepository.Add(tripRoute);

                foreach (var touristObjectId in TouristObjectIds)
                {
                    var tripRouteTouristObject = new TripRouteTouristObject
                    {
                        TripRouteId = tripRoute.Id,
                        TouristObjectId = touristObjectId
                    };
                    context.TripRouteTouristObjects.Add(tripRouteTouristObject);
                }
                context.SaveChanges();
            }

            ViewBag.TouristObjects = touristObjectRepository.GetAll();
            return View("Create");
        }

        public IActionResult Update(Guid id)
        {
            var tripRoute = tripRouteRepository.Get(id);
            if (tripRoute == null)
            {
                return NotFound();
            }

            // Убедитесь, что коллекция инициализирована
            tripRoute.TripRouteTouristObjects = tripRoute.TripRouteTouristObjects ?? new List<TripRouteTouristObject>();

            ViewBag.TouristObjects = touristObjectRepository.GetAll();

            var selectedTouristObjectIds = context.TripRouteTouristObjects
                .Where(to => to.TripRouteId == id)
                .Select(to => to.TouristObjectId)
                .ToList();

            ViewBag.SelectedTouristObjectIds = selectedTouristObjectIds;

            return View(tripRoute);
        }

        [HttpPost]
        public IActionResult Update(TripRoute tripRoute, List<Guid> TouristObjectIds)
        {
            if (ModelState.IsValid)
            {
                var existingTripRoute = tripRouteRepository.Get(tripRoute.Id);
                if (existingTripRoute == null)
                {
                    return NotFound();
                }

                existingTripRoute.Name = tripRoute.Name;

                var oldTripRouteTouristObjects = context.TripRouteTouristObjects
                    .Where(to => to.TripRouteId == existingTripRoute.Id).ToList();
                context.TripRouteTouristObjects.RemoveRange(oldTripRouteTouristObjects);

                if (TouristObjectIds != null && TouristObjectIds.Count > 0)
                {
                    foreach (var touristObjectId in TouristObjectIds)
                    {
                        var tripRouteTouristObject = new TripRouteTouristObject
                        {
                            TripRouteId = existingTripRoute.Id,
                            TouristObjectId = touristObjectId
                        };
                        context.TripRouteTouristObjects.Add(tripRouteTouristObject);
                    }
                }

                context.SaveChanges();

                return RedirectToAction("List");
            }

            ViewBag.TouristObjects = touristObjectRepository.GetAll();
            return View(tripRoute);
        }

        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var result = tripRouteRepository.Delete(id);
            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction("List");
        }

        public IActionResult List()
        {
            var tripRoutes = tripRouteRepository.GetAll();
            return View(tripRoutes);
        }
    }
}