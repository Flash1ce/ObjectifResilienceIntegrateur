﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Linq;
using Gwenael.Domain;
using Gwenael.Domain.Entities;
using Newtonsoft.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System.IO;

namespace Gwenael.Web.Pages
{
    public class CreationTutoModel : PageModel
    {
        private readonly GwenaelDbContext _db;

        [BindProperty(SupportsGet = true)]
        public InputModel Input { get; set; }

        public CreationTutoModel(GwenaelDbContext pDb) => _db = pDb;

        public class InputModel
        {
            [DataType(DataType.Text)]
            public string id { get; set; }
            public string handler { get; set; }

            [Required]
            [DataType(DataType.Text)]
            public string titre { get; set; }
            [Required]
            [DataType(DataType.Text)]
            public int difficulte { get; set; }
            [Required]
            [DataType(DataType.Currency)]
            public double cout { get; set; }
            [Required]
            [DataType(DataType.Text)]
            public int duree { get; set; }
            //[BindProperty(SupportsGet = true)]
            [DataType(DataType.Text)]
            public string cat { get; set; }
            [Required]
            [DataType(DataType.Text)]
            public string intro { get; set; }

            [Required]
            [DataType(DataType.Text)]
            public string nomCategorie { get; set; }
            [Required]
            [DataType(DataType.Text)]
            public string descriptionCategorie { get; set; }
            public List<CategoriesTutos> lstCategories { get; set; }

            public List<RangeeTutos> lstRangeeTutoriels { get; set; }
            public List<Domain.Entities.Tutos> lstTutoriels { get; set; }
            public IFormFile imageRangeeFile { get; set; }
            public IFormFile imageBanierFile { get; set; }
            public string imgBannierUrl { get; set; }
        }

        public IActionResult OnGet()
        {
            Input = new InputModel();

            if (Request.Query.Count == 1)
            {
                Input.handler = Request.Query["handler"];
            }
            else if (Request.Query.Count > 1)
            {
                Input.handler = Request.Query["handler"];
                Input.id = Request.Query["id"];
            }
            UpdateInputData();

            return Page();
        }

        public IActionResult OnPostAsync()
        {
            UpdateInputData();
            return Page();
        }

        public IActionResult OnPostRedirectHomeTuto() => RedirectToPage("Index");

        public class CreationTutoFormData
        {
            public string idTutoP { get; set; }
            public string titre { get; set; }
            public int duree { get; set; }
            public double cout { get; set; }
            public int difficulte { get; set; }
            public IFormFile imageBanierFile { get; set; }
            public string imgUrl { get; set; }
            public string cat { get; set; }
            public string intro { get; set; }
        }

        public IActionResult OnPostCreeTutorielDetails([FromForm] CreationTutoFormData formData)
        {
            try
            {
                formData.imgUrl = null;
                string creationTutoStatus = "false";
                Input.id = formData.idTutoP;
                Input.handler = "CreeTutorielDetails";
                if (!(_db.CategoriesTutos.Where(c => c.Nom == formData.cat).Count() == 0))
                {
                    CategoriesTutos cat = _db.CategoriesTutos.Where(c => c.Nom == formData.cat).First();

                    if (cat != null && _db.Tutos.Where(t => t.Titre == formData.titre).Count() == 0)
                    {
                        if (formData.imageBanierFile != null)
                        {
                            using (var client = new AmazonS3Client("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1))
                            {
                                using (var newMemoryStream = new MemoryStream())
                                {
                                    formData.imageBanierFile.CopyTo(newMemoryStream);
                                    var uploadRequest = new TransferUtilityUploadRequest
                                    {
                                        InputStream = newMemoryStream,
                                        Key = (DateTime.Now.Ticks + formData.imageBanierFile.FileName).ToString(), // filename
                                        BucketName = "mediafileobjectifresiliance", // bucket name of S3
                                        CannedACL = S3CannedACL.PublicRead,
                                    };

                                    var fileTransferUtility = new TransferUtility(client);
                                    fileTransferUtility.Upload(uploadRequest);
                                    formData.imgUrl = ("https://mediafileobjectifresiliance.s3.ca-central-1.amazonaws.com/" + uploadRequest.Key).ToString();
                                }
                            }
                        }
                        Input.imgBannierUrl = formData.imgUrl;
                        Domain.Entities.Tutos tuto = new Domain.Entities.Tutos();
                        tuto.Titre = formData.titre;
                        tuto.Duree = formData.duree;
                        tuto.Cout = formData.cout;
                        tuto.Difficulte = formData.difficulte;
                        tuto.Categorie = cat;
                        tuto.Introduction = formData.intro;
                        tuto.LienImgBanniere = formData.imgUrl;

                        if (tuto.EstValide())
                        {
                            _db.Tutos.Add(tuto);
                            _db.SaveChanges();

                            formData.idTutoP = _db.Tutos.Where(t => t.Titre == tuto.Titre).First().Id.ToString();
                            creationTutoStatus = "true";
                        }
                    }
                }

                UpdateInputData();
                if (creationTutoStatus == "true")
                {
                    return StatusCode(201, new JsonResult(formData));
                }
                else
                {
                    return StatusCode(400, new JsonResult(formData));
                }

            }
            catch (Exception)
            {
                UpdateInputData();
                return StatusCode(400, new JsonResult(formData));
            }
        }

        public IActionResult OnPostModifieTutorielDetails([FromForm] CreationTutoFormData formData)
        {
            try
            {
                string modificationTutoStatus = "false";
                Input.id = formData.idTutoP;
                Input.handler = "CreeTutorielDetails";
                if (!(_db.CategoriesTutos.Where(c => c.Nom == formData.cat).Count() == 0))
                {
                    CategoriesTutos cat = _db.CategoriesTutos.Where(c => c.Nom == formData.cat).First();
                    Domain.Entities.Tutos tuto = _db.Tutos.Where(t => t.Id == Guid.Parse(formData.idTutoP)).First();
                    if (formData.imageBanierFile != null)
                    {
                        using (var client = new AmazonS3Client("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1))
                        {
                            using (var newMemoryStream = new MemoryStream())
                            {
                                formData.imageBanierFile.CopyTo(newMemoryStream);
                                var uploadRequest = new TransferUtilityUploadRequest
                                {
                                    InputStream = newMemoryStream,
                                    Key = (DateTime.Now.Ticks + formData.imageBanierFile.FileName).ToString(), // filename
                                    BucketName = "mediafileobjectifresiliance", // bucket name of S3
                                    CannedACL = S3CannedACL.PublicRead,
                                };

                                var fileTransferUtility = new TransferUtility(client);
                                fileTransferUtility.Upload(uploadRequest);
                                formData.imgUrl = ("https://mediafileobjectifresiliance.s3.ca-central-1.amazonaws.com/" + uploadRequest.Key).ToString();
                            }
                        }
                        Input.imgBannierUrl = formData.imgUrl;
                    } else
                    {
                        formData.imgUrl = tuto.LienImgBanniere;
                        Input.imgBannierUrl = tuto.LienImgBanniere;
                    }
                    
                    if (cat != null && tuto != null)
                    {
                        tuto.Titre = formData.titre;
                        tuto.Duree = formData.duree;
                        tuto.Cout = formData.cout;
                        tuto.Difficulte = formData.difficulte;
                        tuto.Categorie = cat;
                        tuto.Introduction = formData.intro;

                        if (tuto.EstValide())
                        {
                            Input.id = tuto.Id.ToString();

                            _db.Tutos.Update(tuto);
                            _db.SaveChanges();

                            modificationTutoStatus = "true";
                        }
                    }
                }

                UpdateInputData();
                if (modificationTutoStatus == "true")
                {
                    return StatusCode(201, new JsonResult(formData));
                }
                else
                {
                    return StatusCode(400, new JsonResult(formData));
                }
            }
            catch (Exception)
            {
                UpdateInputData();
                return StatusCode(400, new JsonResult(formData));
            }
        }

        public IActionResult OnPostNettoyerTutorielDetails() => Redirect("/tutoriel/CreationTuto");

        public IActionResult OnPostCreationCategorie(string id, string handler)
        {
            try
            {
                string creationCategorieStatus = "false";
                if (_db.CategoriesTutos.Where(c => c.Nom == Input.nomCategorie).Count() == 0)
                {
                    CategoriesTutos cat = new CategoriesTutos();
                    cat.Nom = Input.nomCategorie;
                    cat.Description = Input.descriptionCategorie;

                    if (cat.EstValide())
                    {
                        _db.CategoriesTutos.Add(cat);
                        _db.SaveChanges();

                        Input.descriptionCategorie = "";
                        Input.nomCategorie = "";
                        creationCategorieStatus = "true";
                    }
                }
                Input.id = id;
                Input.handler = handler;
                UpdateInputData();
                return Redirect("/Tutoriel/CreationTuto?handler=CreationCategorie&id=" + Input.id + "&creationCategorieStatus=" + creationCategorieStatus);

            }
            catch (Exception)
            {
                UpdateInputData();
                return Page();
            }
        }

        public IActionResult OnPostAjoutRangee(string rangeeTexte, string positionImage, string id, string handler)
        {
            try
            {
                Input.id = id;
                Input.handler = handler;
                string imgUrl = null;
                // TODO : Faire en sorte de validé si c'est une rangé d'image ou de texte ou les deux........
                if (positionImage == "right" || positionImage == "left")
                {
                    if (Input.imageRangeeFile != null)
                    {
                        using (var client = new AmazonS3Client("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1))
                        {
                            using (var newMemoryStream = new MemoryStream())
                            {
                                Input.imageRangeeFile.CopyTo(newMemoryStream);
                                var uploadRequest = new TransferUtilityUploadRequest
                                {
                                    InputStream = newMemoryStream,
                                    Key = (DateTime.Now.Ticks + Input.imageRangeeFile.FileName).ToString(), // filename
                                    BucketName = "mediafileobjectifresiliance", // bucket name of S3
                                    CannedACL = S3CannedACL.PublicRead,
                                };

                                var fileTransferUtility = new TransferUtility(client);
                                fileTransferUtility.Upload(uploadRequest);
                                imgUrl = ("https://mediafileobjectifresiliance.s3.ca-central-1.amazonaws.com/" + uploadRequest.Key).ToString();
                            }
                        }
                    }

                    RangeeTutos rangee = new RangeeTutos();
                    rangee.TutorielId = Guid.Parse(id);
                    rangee.Texte = rangeeTexte;
                    rangee.PositionImg = positionImage;
                    rangee.LienImg = imgUrl;

                    _db.RangeeTutos.Add(rangee);
                    _db.SaveChanges();
                    Guid rId = _db.RangeeTutos.Where(r => r == rangee).First().Id;

                    Input.lstRangeeTutoriels = _db.RangeeTutos.Where(r => r.TutorielId == Guid.Parse(id)).ToList<RangeeTutos>();
                }

                UpdateInputData();
                return Page();
            }
            catch (Exception)
            {
                UpdateInputData();
                return Page();
            }
        }

        public IActionResult OnPostPublierTutoriel(string id, string handler)
        {
            try
            {
                Input.id = id;
                Input.handler = handler;

                UpdateInputData();
                return Page();
            }
            catch (Exception)
            {
                UpdateInputData();
                return Page();
            }
        }

        public IActionResult OnPostTutoChanger(string tutoId, string handler)
        {
            try
            {
                Input.handler = "AjoutRangee";
                Input.id = tutoId;
                Input.handler = handler;
                UpdateInputData();
                return Redirect("/tutoriel/CreationTuto?handler=TutoChanger&id=" + tutoId);
            }
            catch (Exception)
            {
                UpdateInputData();
                return Redirect("/tutoriel/CreationTuto?handler=TutoChanger");
            }
        }

        public IActionResult OnPostEditRange(string idRangeeVal, string id, string handler)
        {
            try
            {
                Input.handler = "AjoutRangee";
                Input.id = id;

                UpdateInputData();
                return Page();
            }
            catch (Exception)
            {
                UpdateInputData();
                return Page();
            }
        }

        public class Rangee
        {
            public string idRangeeVal { get; set; }
        }

        public IActionResult OnPostDeleteRange(string id, string handler, [FromBody] Rangee rangee)
        {
            try
            {
                Input.handler = "AjoutRangee";
                Input.id = id;

                Domain.Entities.Tutos t = _db.Tutos.Where(t => t.Id == Guid.Parse(id) && t.EstPublier == false).First();
                if (t != null)
                {
                    RangeeTutos rt = _db.RangeeTutos.Where(r => r.TutorielId == Guid.Parse(id) && r.Id == Guid.Parse(rangee.idRangeeVal)).First();
                    if (rt != null)
                    {

                        _db.RangeeTutos.Remove(rt);
                        _db.SaveChanges();
                    }
                }

                UpdateInputData();
                return Page();
            }
            catch (Exception)
            {
                UpdateInputData();
                return Page();
            }
        }

        public IActionResult OnPostPublieTuto(string id, string handler)
        {
            try
            {
                Input.handler = "AjoutRangee";
                Input.id = id;

                Domain.Entities.Tutos t = _db.Tutos.Where(t => t.Id == Guid.Parse(id) && t.EstPublier == false).First();
                if (t != null)
                {
                    t.EstPublier = true;
                    _db.Tutos.Update(t);
                    _db.SaveChanges();
                }

                UpdateInputData();
                return Redirect("/Tutoriel/Consultation?id=" + id + "&estPublie=true");
            }
            catch (Exception)
            {
                UpdateInputData();
                return Page();
            }
        }

        public void UpdateInputData()
        {
            Input.lstCategories = _db.CategoriesTutos.ToList<CategoriesTutos>();
            if (!String.IsNullOrEmpty(Input.id))
            {
                Input.lstRangeeTutoriels = _db.RangeeTutos.Where(r => r.TutorielId == Guid.Parse(Input.id)).ToList<RangeeTutos>();
            }
            Input.lstTutoriels = _db.Tutos.Where(t => t.EstPublier == false).ToList<Domain.Entities.Tutos>();

            if (!String.IsNullOrEmpty(Input.id))
            {
                Domain.Entities.Tutos tuto = _db.Tutos.Where(t => t.Id == Guid.Parse(Input.id)).First();
                if (tuto != null)
                {
                    Input.titre = tuto.Titre;
                    Input.duree = tuto.Duree;
                    Input.cout = tuto.Cout;
                    Input.difficulte = tuto.Difficulte;
                    Input.intro = tuto.Introduction;
                    Input.cat = tuto.Categorie.Nom;
                    Input.imgBannierUrl = tuto.LienImgBanniere;
                }
            }
        }
    }
}