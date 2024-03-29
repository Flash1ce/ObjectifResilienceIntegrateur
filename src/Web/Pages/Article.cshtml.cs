using Gwenael.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Gwenael.Domain;
using System.Linq;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.IO;
using Gwenael.Web.FctUtils;

namespace Gwenael.Web.Pages
{
    public class ArticleModel : PageModel
    {
        private readonly GwenaelDbContext _context;
        private IList<NewPage> NewPages { get; set; }


        public ArticleModel(GwenaelDbContext context)
        {
            _context = context;
        }
        public List<Article> Articles { get; set; }
        public Article article { get; set; }
        public List<Article> b;
        public List<Article> a;

        public async Task<IActionResult> OnGet()
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


            Articles = await _context.Articles.ToListAsync();
            NewPages = await _context.NewPages.ToListAsync();
            ViewData["NewPages"] = NewPages;

            string id = Request.Query["id"];
            int intId = Int32.Parse(id);


            Article b = Articles.Where(article => article.Id == intId).First();
            ViewData["tst"] = "tst";
            ViewData["unArticle"] = b;
            var s3Client = new AmazonS3Client("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1);
            var buckets = await s3Client.ListBucketsAsync();



            foreach (var bucket in buckets.Buckets)
            {
                var objects = await s3Client.ListObjectsAsync(bucket.BucketName);

                if (objects != null)
                {
                    var tst = objects.S3Objects;
                    ViewData["unObjet"] = tst;


                    var s3Requ = new Amazon.S3.Model.GetObjectRequest
                    {
                        BucketName = bucket.BucketName,
                        Key = string.Join(",", objects.S3Objects[0].Key)
                    };

                    var s3Resp = s3Client.GetObjectAsync(s3Requ);
                    var res = s3Resp.Result;

                }
            }



            return Page();
        }

    }
}
