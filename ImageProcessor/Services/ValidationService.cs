using Microsoft.Extensions.Logging;

namespace ImageProcessor.Services;

public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(
        ILogger<ValidationService> logger
    )
    {
        _logger = logger;
    }
    
    public bool IsImage(Stream blobBytes)
    {
        if (blobBytes == null || !blobBytes.CanRead)
        {
            _logger.LogWarning("Cannot determine if the file is an image. The stream was null or not readable");
            return false;
        } else
        {
            byte[] signatureBytes = new byte[8];
            int readBytesCount = blobBytes.Read(signatureBytes, 0, signatureBytes.Length);

            _logger.LogWarning($"Signature bytes are: {BitConverter.ToString(signatureBytes)}");

            if (readBytesCount < 3)
            {
                _logger.LogWarning("File is not a valid image; invalid byte signature.");
                return false;
            } else
            {
                bool isPng = IsJPEG(signatureBytes);
                if (isPng)
                {
                    _logger.LogWarning("File is a valid JPEG image.");
                    return isPng;
                } else
                {
                    bool isJpg = IsPNG(signatureBytes);
                    if (isJpg)
                    {
                        _logger.LogWarning("File is a valid PNG image.");
                    }
                    else
                    {
                        _logger.LogWarning("File is not a valid image.");
                    }

                    return isJpg;
                }
            }
        }
    }

    public bool IsValidSize(Stream blobBytes)
    {
        throw new NotImplementedException();
    }

    private bool IsJPEG(byte[] signatureBytes)
    {
        if (
            signatureBytes[0] == 0xFF &&
            signatureBytes[1] == 0xD8 &&
            signatureBytes[2] == 0xFF
        )
        {
            return true;
        } else
        {
            return false;
        }
    }

    private bool IsPNG(byte[] signatureBytes)
    {
        if (
            signatureBytes[0] == 0x89 &&
            signatureBytes[1] == 0x50 &&
            signatureBytes[2] == 0x4E &&
            signatureBytes[3] == 0x47 &&
            signatureBytes[4] == 0x0D &&
            signatureBytes[5] == 0x0A &&
            signatureBytes[6] == 0x1A &&
            signatureBytes[7] == 0x0A
        )
        {
            return true;
        } else
        {
            return false;
        }
    }
}
