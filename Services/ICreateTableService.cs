namespace Game29Prices.Services;

public interface ICreateTableService
{
    Task<byte[]> CreateTableAsync();
}