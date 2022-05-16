﻿using Gwenael.Domain;
using Gwenael.Domain.Entities;
using Gwenael.Web.FctUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Spk.Common.Helpers.String;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace Gwenael.Web.Pages
{
    public class ConsultationModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public InputModel Input { get; set; }

        private readonly GwenaelDbContext _db;
        public IList<NewPage> NewPages { get; set; }

        public class InputModel
        {
            public Tutos tutoriel { get; set; }
            public List<RangeeTutos> lstRangeeTuto { get; set; }
            public string id { get; set; }
            public bool droitAccess { get; set; }
        }
        public ConsultationModel(GwenaelDbContext pDb)
        {
            _db = pDb;
        }

        public Guid ObtenirIdDuUserSelonEmail(string email)
        {
            User user = (User)_db.Users.Where(u => u.UserName == email).First();
            return user.Id;
        }
        public IActionResult OnGet()
        {
            NewPages = _db.NewPages.ToList();
            ViewData["NewPages"] = NewPages;

            Input = new InputModel();

            Input.droitAccess = false;
            if (User.Identity.IsAuthenticated)
            {
                Guid idConnectedUser = ObtenirIdDuUserSelonEmail(User.Identity.Name);
                if (Permission.VerifierAccesGdC(idConnectedUser, _db))
                {
                    Input.droitAccess = true;
                }
            }

            if (Request.Query.Count >= 1)
            {
                Input.id = Request.Query["id"];
                if (IdEstValide() && Input.droitAccess==true)
                {
                    return Page();
                } else if(IdEstValide() && Input.droitAccess==false && EstPublie())
                {
                    return Page();
                }
            }

            return Redirect("/tutoriel");
        }

        public IActionResult OnPost()
        {
            return Page();
        }

        public IActionResult OnPostRedirectHomeTuto()
        {
            return RedirectToPage("Index");
        }

        public IActionResult OnPostUnpublishTuto([FromBody] TutorielIdVal tutoVal)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    Guid idConnectedUser = ObtenirIdDuUserSelonEmail(User.Identity.Name);
                    if (Permission.VerifierAccesGdC(idConnectedUser, _db))
                    {
                        Console.WriteLine(tutoVal);

                        Tutos t = _db.Tutos.Where(t => t.Id == Guid.Parse(tutoVal.tutorielIdVal)).First();
                        
                        bool status = false;
                        if (!t.EstPublier)
                            status = true;

                        _db.Tutos.Where(t => t.Id == Guid.Parse(tutoVal.tutorielIdVal)).First().EstPublier = status;
                        _db.SaveChanges();

                        return Redirect("/tutoriel?unPublishStatus=" + status);
                    }
                }
                return Redirect("/tutoriel");
            }
            catch (Exception)
            {
                return Redirect("/tutoriel");
            }
        }

        public class TutorielIdVal
        {
            public string tutorielIdVal { get; set; }
        }

        private bool IdEstValide()
        {
            if (!Input.id.IsNullOrEmpty())
            {
                if (_db.Tutos.Where(t => t.Id == Guid.Parse(Input.id)).Any())
                {
                    Input.tutoriel = _db.Tutos.Where(t => t.Id == Guid.Parse(Input.id)).First();
                    GetContenue();
                    return true;
                }
            }
            return false;
        }

        private bool EstPublie()
        {
            if (Input.tutoriel.EstPublier)
            {
                return true;
            }
            return false;
        }

        private void GetContenue()
        {
            Input.lstRangeeTuto = _db.RangeeTutos.Where(r => r.TutorielId == Guid.Parse(Input.id)).ToList();
        }
    }
}