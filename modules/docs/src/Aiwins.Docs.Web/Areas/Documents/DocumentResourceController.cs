﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Aiwins.Rocket;
using Aiwins.Rocket.AspNetCore.Mvc;
using Aiwins.Rocket.Http;
using Aiwins.Rocket.IO;
using Aiwins.Docs.Documents;

namespace Aiwins.Docs.Areas.Documents
{
    [RemoteService]
    [Area("docs")]
    [ControllerName("DocumentResource")]
    [Route("document-resources")]
    public class DocumentResourceController : RocketController
    {
        private readonly IDocumentAppService _documentAppService;

        public DocumentResourceController(IDocumentAppService documentAppService)
        {
            _documentAppService = documentAppService;
        }

        [HttpGet]
        [Route("")]
        public async Task<FileResult> GetResource(GetDocumentResourceInput input)
        {
            input.Name = input.Name.RemovePreFix("/");
            var documentResource = await _documentAppService.GetResourceAsync(input);
            var contentType = MimeTypes.GetByExtension(FileHelper.GetExtension(input.Name));
            return File(documentResource.Content, contentType);
        }
    }
}