﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using NuGet.Packaging;
using NuGet.Protocol;
using OfficeOpenXml;
using Spk.Common.Helpers.String;


namespace Gwenael.Web.Pages
{
    public class InviteFormationModel : PageModel
    {
        private IHostingEnvironment _environment;

        [BindProperty]
        public InputModel Input { get; set; }

        public InviteFormationModel(IHostingEnvironment environement)
        {
            _environment = environement;
        }

        public class InputModel
        {
            [EmailAddress]
            public string Email { get; set; }
            public List<EmailFormation> lstEmailFormation { get; set; }
            public IFormFile FormFile { get; set; }

        }
        public void OnGet()
        {
            Input = new InputModel();
            Console.WriteLine("GET-----------------");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input.FormFile != null && Input.FormFile.FileName.EndsWith(".csv"))
            {
                List<EmailFormation> list = new List<EmailFormation>();

                using (var reader = new System.IO.StreamReader(Input.FormFile.OpenReadStream()))
                {
                    string result = reader.ReadToEnd();
                    result = result.Replace("\r", "");
                    string[] vectLignes = result.Split('\n');
                    int nbLignes = vectLignes.Length - 2;

                    if (vectLignes[vectLignes.Length - 1] != "")
                    {
                        nbLignes = vectLignes.Length - 1;
                    }

                    string formation = "FormationPlaceHolder";

                    for (int i = 0; i <= nbLignes; i++)
                    {
                        string[] vectChamps = vectLignes[i].Split(';');
                        if(vectChamps[0] != "" && vectChamps[0].Length > 1)
                            list.Add(new EmailFormation(formation, vectChamps[0]));
                    }

                }
                Input.lstEmailFormation = list;


                //// TODO: Réusir a importer un fichier excel a la place d'un csv.
                //string path = _environment.ContentRootPath + "\\wwwroot\\fichiers\\uploadsExcel\\" + DateTime.Now.Ticks + Input.FormFile.FileName;

                //using (var stream = new FileStream(path, FileMode.Create))
                //{
                //    await Input.FormFile.CopyToAsync(stream);

                //    Console.WriteLine("ONPOSTASYNC-------------------");
                //    using (ExcelPackage package = new ExcelPackage(new FileInfo(path)))
                //    {
                //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                //        var sheet = package.Workbook.Worksheets[0];

                //        Input.lstEmailFormation = GetExcelEmail(sheet);
                //    }
                //}

                //FileInfo file = new FileInfo(path);
                //if (file.Exists)
                //{
                //    file.Delete();
                //}
            }

            return Page();
        }

        //public List<EmailFormation> GetExcelEmail(ExcelWorksheet sheet)
        //{
        //    //List<EmailFormation> list = new List<EmailFormation>();
        //    Console.WriteLine("GETEXCEL EMAIL------------------------");
        //    List<EmailFormation> list = new List<EmailFormation>();
        //    var columnInfo = Enumerable.Range(1, sheet.Dimension.Columns).ToList().Select(n =>
        //        new { Index = n, ColumnName = sheet.Cells[1, n].Value.ToString() }
        //    );

        //    var formation = (string)sheet.Cells[2, 2].Value;
        //    for (int row = 3; row < sheet.Dimension.Rows; row++)
        //    {
        //        var contenue = (string)sheet.Cells[row, 2].Value;
        //        if (!contenue.IsNullOrEmpty() && contenue != " " && contenue != "")
        //        {
        //            list.Add(new EmailFormation(formation, contenue));
        //        }
        //    }

        //    return list;
        //}

        public class EmailFormation
        {
            private string _formation;
            private string _email;

            public EmailFormation(string formation, string email)
            {
                _formation = formation;
                _email = email;
            }

            public string GetEmail()
            {
                return _email;
            }

            public string GetFormation()
            {
                return _formation;
            }
        }
    }
}
