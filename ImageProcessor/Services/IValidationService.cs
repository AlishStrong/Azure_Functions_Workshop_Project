using System;

namespace ImageProcessor.Services;

public interface IValidationService
{
    public bool IsImage(Stream bytesStream);
    public bool IsValidSize(Stream bytesStream);
}
