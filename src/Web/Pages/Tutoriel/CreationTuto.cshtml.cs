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

            if (Request.Query.Count == 1) Input.handler = Request.Query["handler"];
            else if (Request.Query.Count > 1)
                Input.handler = Request.Query["handler"]; Input.id = Request.Query["id"];

            UpdateInputData();
            return Page();
        }

        public IActionResult OnPostAsync() { UpdateInputData(); return Page(); }

        public IActionResult OnGetRangeeById(string idRangee)
        {
            try
            {
                Input.handler = "TutoRangee";
                RangeeTutos rt = _db.RangeeTutos.Where(r => r.Id == Guid.Parse(idRangee)).First();
                return rt != null ? StatusCode(200, new JsonResult(rt)) : StatusCode(400);
            }
            catch (Exception)
            {
                return StatusCode(400); ;
            }
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
                bool creationTutoStatus = false;
                Input.id = formData.idTutoP;
                Input.handler = "CreeTutorielDetails";
                if (!(!_db.CategoriesTutos.Where(c => c.Nom == formData.cat).Any()))
                {
                    CategoriesTutos cat = _db.CategoriesTutos.Where(c => c.Nom == formData.cat).First();

                    if (cat != null && _db.Tutos.Where(t => t.Titre == formData.titre).Count() == 0)
                    {
                        if (formData.imageBanierFile != null)
                        {
                            using (AmazonS3Client client = new("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1))
                            {
                                using MemoryStream newMemoryStream = new();
                                formData.imageBanierFile.CopyTo(newMemoryStream);
                                var uploadRequest = new TransferUtilityUploadRequest
                                {
                                    InputStream = newMemoryStream,
                                    Key = (DateTime.Now.Ticks + formData.imageBanierFile.FileName).ToString(), // filename
                                    BucketName = "mediafileobjectifresiliance", // bucket name of S3
                                    CannedACL = S3CannedACL.PublicReadWrite
                                };

                                TransferUtility fileTransferUtility = new(client);
                                fileTransferUtility.Upload(uploadRequest);
                                formData.imgUrl = $"https://mediafileobjectifresiliance.s3.ca-central-1.amazonaws.com/{uploadRequest.Key}";
                            }
                        }
                        Input.imgBannierUrl = formData.imgUrl;

                        Tutos tuto = new();
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
                            creationTutoStatus = true;
                        }
                    }
                }

                UpdateInputData();
                return creationTutoStatus ? StatusCode(201, new JsonResult(formData)) : StatusCode(400, new JsonResult(formData));
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
                bool modificationTutoStatus = false;
                Input.id = formData.idTutoP;
                Input.handler = "CreeTutorielDetails";
                if (!(!_db.CategoriesTutos.Where(c => c.Nom == formData.cat).Any()))
                {
                    CategoriesTutos cat = _db.CategoriesTutos.Where(c => c.Nom == formData.cat).First();
                    Tutos tuto = _db.Tutos.Where(t => t.Id == Guid.Parse(formData.idTutoP)).First();
                    if (formData.imageBanierFile != null)
                    {
                        using (AmazonS3Client client = new("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1))
                        {
                            using MemoryStream newMemoryStream = new();
                            formData.imageBanierFile.CopyTo(newMemoryStream);
                            var uploadRequest = new TransferUtilityUploadRequest
                            {
                                InputStream = newMemoryStream,
                                Key = (DateTime.Now.Ticks + formData.imageBanierFile.FileName).ToString(), // filename
                                BucketName = "mediafileobjectifresiliance", // bucket name of S3
                                CannedACL = S3CannedACL.PublicReadWrite
                            };

                            TransferUtility fileTransferUtility = new(client);
                            fileTransferUtility.Upload(uploadRequest);
                            formData.imgUrl = $"https://mediafileobjectifresiliance.s3.ca-central-1.amazonaws.com/{uploadRequest.Key}";
                        }
                        Input.imgBannierUrl = formData.imgUrl;
                        tuto.LienImgBanniere = formData.imgUrl;
                    }
                    else
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

                            modificationTutoStatus = true;
                        }
                    }
                }

                UpdateInputData();
                return modificationTutoStatus ? StatusCode(201, new JsonResult(formData)) : StatusCode(400, new JsonResult(formData));
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
                string status = "false";
                if (!_db.CategoriesTutos.Where(c => c.Nom == Input.nomCategorie).Any())
                {
                    CategoriesTutos cat = new();
                    cat.Nom = Input.nomCategorie;
                    cat.Description = Input.descriptionCategorie;

                    if (cat.EstValide())
                    {
                        _db.CategoriesTutos.Add(cat);
                        _db.SaveChanges();

                        Input.descriptionCategorie = "";
                        Input.nomCategorie = "";
                        status = "true";
                    }
                }
                Input.id = id;
                Input.handler = handler;

                UpdateInputData();
                return Redirect($"/Tutoriel/CreationTuto?handler=CreationCategorie&id={Input.id}&creationCategorieStatus={status}");
            }
            catch (Exception)
            {
                UpdateInputData();
                return Page();
            }
        }

        public class CreationTutoRangeeFormData
        {
            public string idRangee { get; set; }
            public string idTutoP { get; set; }
            public string inputTitreEtape { get; set; }
            public string rangeeTexte { get; set; }
            public string positionImage { get; set; }
            public string imageUrl { get; set; }
            public bool cbRetirerImage { get; set; }
            public IFormFile imageRangeeFile { get; set; }
        }

        public IActionResult OnPostAjoutRangee([FromForm] CreationTutoRangeeFormData formData)
        {
            try
            {
                bool estAjoutee = false;
                Input.id = formData.idTutoP;
                Input.handler = "TutoRangee";
                string imgUrl = null;

                if (formData.positionImage != "right" && formData.positionImage != "left") formData.positionImage = "left";

                if (Input.imageRangeeFile != null)
                    using (AmazonS3Client client = new("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1))
                    {
                        using MemoryStream newMemoryStream = new();
                        Input.imageRangeeFile.CopyTo(newMemoryStream);
                        TransferUtilityUploadRequest uploadRequest = new()
                        {
                            InputStream = newMemoryStream,
                            Key = (DateTime.Now.Ticks + Input.imageRangeeFile.FileName).ToString(), // filename
                            BucketName = "mediafileobjectifresiliance", // bucket name of S3
                            CannedACL = S3CannedACL.PublicReadWrite
                        };

                        TransferUtility fileTransferUtility = new(client);
                        fileTransferUtility.Upload(uploadRequest);
                        imgUrl = $"https://mediafileobjectifresiliance.s3.ca-central-1.amazonaws.com/{uploadRequest.Key}";
                    }

                RangeeTutos rangee = new();
                rangee.TutorielId = Guid.Parse(formData.idTutoP);
                rangee.Titre = formData.inputTitreEtape;
                rangee.Texte = formData.rangeeTexte;
                rangee.PositionImg = formData.positionImage;
                rangee.LienImg = imgUrl;

                _db.RangeeTutos.Add(rangee);
                _db.SaveChanges();
                Guid rId = _db.RangeeTutos.Where(r => r == rangee).First().Id;

                Input.lstRangeeTutoriels = _db.RangeeTutos.Where(r => r.TutorielId == Guid.Parse(formData.idTutoP)).ToList();
                estAjoutee = true;
                formData.idRangee = rId.ToString();

                UpdateInputData();
                return estAjoutee ? StatusCode(201, new JsonResult(formData)) : StatusCode(400, new JsonResult(formData));
            }
            catch (Exception)
            {
                UpdateInputData();
                return StatusCode(400, new JsonResult(formData));
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

        public IActionResult OnPostTutoChanger(string tutoId)
        {
            try
            {
                Input.handler = "TutoRangee";
                Input.id = tutoId;

                UpdateInputData();
                return Redirect("/tutoriel/CreationTuto?handler=TutoRangee&id=" + tutoId);
            }
            catch (Exception)
            {
                UpdateInputData();
                return Redirect("/tutoriel/CreationTuto?handler=TutoRangee");
            }
        }

        public IActionResult OnPutEditRangee([FromForm] CreationTutoRangeeFormData formData)
        {
            try
            {
                bool estEdit = false;
                Input.id = formData.idTutoP;
                Input.handler = "TutoRangee";

                if (formData.positionImage != "right" && formData.positionImage != "left") formData.positionImage = "left";

                RangeeTutos r = _db.RangeeTutos.Where(r => r.Id == Guid.Parse(formData.idRangee)).First();

                if (formData.cbRetirerImage == true)
                    r.LienImg = null;
                if (formData.cbRetirerImage != true && Input.imageRangeeFile != null)
                {
                    //if (r.LienImg != null) RemoveImgS3Amazone(r.LienImg);
                    using AmazonS3Client client = new("AKIAVDH3AEDD6PUJMKGG", "kKV5WKu0tFe8Svl2QdTIMIydLc7CGSMiy2h+KOvV", RegionEndpoint.CACentral1);
                    using (MemoryStream newMemoryStream = new())
                    {
                        Input.imageRangeeFile.CopyTo(newMemoryStream);
                        TransferUtilityUploadRequest uploadRequest = new()
                        {
                            InputStream = newMemoryStream,
                            Key = (DateTime.Now.Ticks + Input.imageRangeeFile.FileName).ToString(), // filename
                            BucketName = "mediafileobjectifresiliance", // bucket name of S3
                            CannedACL = S3CannedACL.PublicReadWrite
                        };

                        TransferUtility fileTransferUtility = new(client);
                        fileTransferUtility.Upload(uploadRequest);
                        r.LienImg = $"https://mediafileobjectifresiliance.s3.ca-central-1.amazonaws.com/{uploadRequest.Key}";
                    }
                }

                formData.imageUrl = r.LienImg;

                r.Titre = formData.inputTitreEtape;
                r.Texte = formData.rangeeTexte;
                r.PositionImg = formData.positionImage;

                _db.RangeeTutos.Update(r);
                _db.SaveChanges();

                Guid rId = _db.RangeeTutos.Where(rt => rt == r).First().Id;

                Input.lstRangeeTutoriels = _db.RangeeTutos.Where(rt => rt.TutorielId == Guid.Parse(formData.idTutoP)).ToList();
                estEdit = true;
                formData.idRangee = rId.ToString();

                UpdateInputData();
                return estEdit ? StatusCode(201, new JsonResult(formData)) : StatusCode(400, new JsonResult(formData));
            }
            catch (Exception)
            {
                UpdateInputData();
                return StatusCode(400, new JsonResult(formData));
            }
        }

        public class Rangee
        {
            public string IdRangeeVal { get; set; }
            public string IdtutoVal { get; set; }
        }

        public IActionResult OnDeleteDeleteRange([FromForm] Rangee r)
        {
            try
            {
                Input.handler = "TutoRangee";
                Input.id = r.IdtutoVal;
                bool estSupprimer = false;
                Tutos t = _db.Tutos.Where(t => t.Id == Guid.Parse(r.IdtutoVal) && t.EstPublier == false).First();
                if (t != null)
                {
                    RangeeTutos rt = _db.RangeeTutos.Where(rt => rt.TutorielId == Guid.Parse(r.IdtutoVal) && rt.Id == Guid.Parse(r.IdRangeeVal)).First();
                    if (rt != null)
                    {
                        _db.RangeeTutos.Remove(rt);
                        _db.SaveChanges();
                        estSupprimer = true;
                    }
                }

                UpdateInputData();
                return estSupprimer ? StatusCode(202, new JsonResult(r)) : StatusCode(400, new JsonResult(r));
            }
            catch (Exception)
            {
                UpdateInputData();
                return StatusCode(400, new JsonResult(r));
            }
        }

        public IActionResult OnPostPublieTuto(string id)
        {
            try
            {
                Input.id = id;

                Tutos t = _db.Tutos.Where(t => t.Id == Guid.Parse(id) && t.EstPublier == false).First();
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
        public class rangeeSwitchData {
            public string IdOld { get; set; }
            public string IdNew { get; set; }
        }
        public IActionResult OnPostSwitchRangeeTuto([FromForm] rangeeSwitchData rangeeSwitchData)
        {
            try
            {
                RangeeTutos oldRt = _db.RangeeTutos.Where(rt => rt.Id == Guid.Parse(rangeeSwitchData.IdOld)).First();
                RangeeTutos newRt = _db.RangeeTutos.Where(rt => rt.Id == Guid.Parse(rangeeSwitchData.IdNew)).First();

                string text = oldRt.Texte;
                string titre = oldRt.Titre;
                string positionImg = oldRt.PositionImg;
                string lienImg = oldRt.LienImg;

                oldRt.Texte = newRt.Texte;
                oldRt.Titre = newRt.Titre;
                oldRt.PositionImg = newRt.PositionImg;
                oldRt.LienImg = newRt.LienImg;
                _db.RangeeTutos.Update(oldRt);

                newRt.Texte = text;
                newRt.Titre = titre;
                newRt.PositionImg = positionImg;
                newRt.LienImg = lienImg;
                _db.RangeeTutos.Update(newRt);

                _db.SaveChanges();

                return StatusCode(200);
            }
            catch (Exception)
            {
                return StatusCode(400);
            }
        }

        public void UpdateInputData()
        {
            Input.lstCategories = _db.CategoriesTutos.ToList();
            if (!String.IsNullOrEmpty(Input.id))
                Input.lstRangeeTutoriels = _db.RangeeTutos.Where(r => r.TutorielId == Guid.Parse(Input.id)).ToList();

            Input.lstTutoriels = _db.Tutos.Where(t => t.EstPublier == false).ToList();

            if (!String.IsNullOrEmpty(Input.id))
            {
                Tutos tuto = _db.Tutos.Where(t => t.Id == Guid.Parse(Input.id)).First();
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