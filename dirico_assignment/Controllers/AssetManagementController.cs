﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using dirico_assignment.Models.AssetManagementModel;
using System.IO;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

namespace dirico_assignment.Controllers
{
    //[EnableCors(origins: "*", headers: "*", methods: "*",exposedHeaders: "X-Custom-Header")]
    [RoutePrefix("api/assetmanagement")]
    public class AssetManagementController : ApiController
    {
        [HttpGet]
        public IHttpActionResult GetFolderStructure()
        {
            IList<FolderStructureModel> folderstructure = null;
            using (var entities = new Dirico_DatabaseEntities())
            {
                var result = entities.FolderStructures.Select(fs => new FolderStructureModel
                {
                    id = fs.ID,
                    name = fs.Name,
                    isDirectory = fs.IsDirectory,
                    parentID = fs.ParentID,
                    blobName = entities.Assets.Where(x => x.FileObjectId == fs.ID).Select(x => x.BlobName).FirstOrDefault(),
                    title = entities.Assets.Where(x => x.FileObjectId == fs.ID).Select(x => x.Title).FirstOrDefault(),
                    content = entities.Assets.Where(x => x.FileObjectId == fs.ID).Select(x => x.Content).FirstOrDefault(),
                    size = entities.Assets.Where(x => x.FileObjectId == fs.ID).Select(x => x.Size).FirstOrDefault()
                }
                ).ToList();
                foreach (var i in result)
                    i.items = result.Where(n => n.parentID == i.id).ToList();

                folderstructure = result.Where(x => x.parentID.HasValue == false).
                    //Join(entities.Assets, fs => fs.id, As => As.FileObjectId, (fs, As) => new { fs, As }).
                    //Select(final => new
                    //FolderStructureModel
                    //{
                    //    id = final.fs.id,
                    //    name = final.fs.name,
                    //    isDirectory = final.fs.isDirectory,
                    //    parentID = final.fs.parentID,
                    //    blobName = final.As.BlobName,
                    //    title = final.As.Title,
                    //    content = final.As.Content,
                    //    size = final.As.Size
                    //}).

                    ToList();
            }
            if (folderstructure.Count == 0)
            {
                return NotFound();
            }
            return Ok(folderstructure);
        }

        [HttpGet]
        [Route("getbyid")]
        public IHttpActionResult CreateNewAsset1(int id)
        {
            var res = id;
            throw new NotImplementedException();
        }
        
        [HttpPost]
        [Route("add_asset")]
        public IHttpActionResult CreateNewAsset([FromBody] AssetModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            using (var entities = new Dirico_DatabaseEntities())
            {
                try
                {
                    //STEP 1 : Insert into File structure
                    var file = entities.FolderStructures.Add(new FolderStructure()
                    {
                        Name = request.name,
                        ParentID = request.parent_id,
                        IsDirectory = false
                    });
                    entities.SaveChanges();
                    //STEP 2: Upload asset to Azure Blob
                    //UploadToBlob(reqMdl.)

                    //STEP 3: Insert into Asset table
                    entities.Assets.Add(new Asset()
                    {
                        FullName = request.name,
                        AssetTypeId = request.assetTypeId,
                        FileObjectId = file.ID,
                        Size = request.size,
                        Title = request.title,
                        Content = request.content,
                        BlobName = request.blobName
                    });
                    entities.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            string message = string.Format("Error '{0}' occurred in {1} at {2}",
                                     subItem.ErrorMessage, entityTypeName, subItem.PropertyName);
                            return BadRequest(message);
                        }
                    }
                }
            }
            return Ok();
        }        

        
        [HttpPost]
        [Route("add_variant")]
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        public IHttpActionResult CreateNewVariant([FromBody] VariantModel request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            IList<FolderStructureModel> folderstructure = new List<FolderStructureModel>();
            using (var entities = new Dirico_DatabaseEntities())
            {
                try
                {
                    //STEP 1 : Insert into File structure
                    var file = entities.FolderStructures.Add(new FolderStructure()
                    {
                        Name = request.name,
                        ParentID = request.parent_id,
                        IsDirectory = true
                    });
                    entities.SaveChanges();

                    ////STEP 2: Insert into Variant table the specific info
                    entities.Variants.Add(new Variant()
                    {
                        FileObjectId = file.ID,
                        //Properties=request.properties
                    });
                    entities.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                    {
                        DbEntityEntry entry = item.Entry;
                        string entityTypeName = entry.Entity.GetType().Name;
                        foreach (DbValidationError subItem in item.ValidationErrors)
                        {
                            string message = string.Format("Error '{0}' occurred in {1} at {2}",
                                     subItem.ErrorMessage, entityTypeName, subItem.PropertyName);
                            return BadRequest(message);
                        }
                    }
                }
            }
            return Ok(folderstructure);
        }


    }
}
