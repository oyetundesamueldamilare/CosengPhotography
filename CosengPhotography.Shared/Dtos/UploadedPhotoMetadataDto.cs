using System;
using System.Collections.Generic;
using System.Text;

namespace CosengPhotography.Shared.Dtos
{
    public class UploadedPhotoMetadataDto
    {
        public string BlobUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}
