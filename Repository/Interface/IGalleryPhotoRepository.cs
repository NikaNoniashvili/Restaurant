using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IGalleryPhotoRepository
{
    List<GalleryPhoto> GetAll();
}