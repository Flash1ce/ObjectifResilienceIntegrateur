﻿using Gwenael.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Gwenael.Domain;
using Gwenael.Web.FctUtils;

namespace Gwenael.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly GwenaelDbContext _context;

        public IndexModel(GwenaelDbContext context)
        {
            _context = context;
        }
        public IList<Article> Articles { get; set; }
        public Article article { get; set; }

        public IList<NewPage> NewPages { get; set; }


        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                Guid idConnectedUser = FctUtils.Permission.ObtenirIdDuUserSelonEmail(User.Identity.Name, _context);
                if (Permission.EstAdministrateur(idConnectedUser, _context))
                {
                    ViewData["estAdmin"] = "true";
                }
            }
            else
            {
                ViewData["estAdmin"] = "false";
            }


            try
            {
                if(_context.Articles != null)
                {
                    Articles = await _context.Articles.ToListAsync();
                    NewPages = await _context.NewPages.ToListAsync();
                }
                else
                {
                    Articles = new List<Article>();
                    NewPages = new List<NewPage>();
                }
                
                ViewData["NewPages"] = NewPages;
                ViewData["lstArticles"] = Articles;
                return Page();

            }
            catch (Exception)
            {
                return Page();
            }
        }

    }
}
