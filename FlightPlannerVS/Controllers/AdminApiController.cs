﻿using System.Linq;
using FlightPlannerVS.Models;
using FlightPlannerVS.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightPlannerVS.Controllers
{
    [Route("admin-api")]
    [ApiController]
    [Authorize]

    public class AdminApiController : ControllerBase
    {
        private static readonly object _locker = new();
        private readonly FlightPlannerDbContext _context;

        public AdminApiController(FlightPlannerDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("Flights/{id}")]
        [Authorize]
        public IActionResult GetFlights(int id)
        {
            var flight = _context.Flights
                .Include(f => f.From)
                .Include(f => f.To)
                .SingleOrDefault(f => f.Id == id);

            if (flight == null) return NotFound();

            return Ok(flight);
        }

        [HttpDelete]
        [Route("Flights/{id}")]
        [Authorize]
        public IActionResult DeleteFlights(int id)
        {
            var flight = _context.Flights
                .Include(f => f.From)
                .Include(f => f.To)
                .SingleOrDefault(f => f.Id == id);

            if (flight != null)
            {
                _context.Flights.Remove(flight);
                _context.SaveChanges();
            }

            return Ok();
        } 

        [HttpPut, Authorize]
        [Route("flights")]
        public IActionResult AddFlight(AddFlightRequest request)
        {
            lock (_locker)
            {
                if (!FlightStorage.IsValid(request)) return BadRequest();

                if (Exists(request)) return Conflict();

                var flight = FlightStorage.ConvertToFlight(request);
                _context.Flights.Add(flight);
                _context.SaveChanges();

                return Created("", flight);
            }
        }

        private bool Exists(AddFlightRequest request)
        {
            return _context.Flights.Any(f => f.DepartureTime == request.DepartureTime 
                                             && f.ArrivalTime == request.ArrivalTime
                                             && f.From.AirportName.ToLower().Trim() == request.From.AirportName.ToLower().Trim()
                                             && f.To.AirportName.ToLower().Trim() == request.To.AirportName.ToLower().Trim());
        }
    }
}
