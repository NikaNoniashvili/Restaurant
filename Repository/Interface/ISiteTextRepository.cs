using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface ISiteTextRepository
{
    List<SiteText> GetAll();
}