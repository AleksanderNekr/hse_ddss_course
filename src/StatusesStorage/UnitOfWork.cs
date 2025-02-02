namespace StatusesStorage;

public class UnitOfWork
{
	private readonly AppDbContext _dbContext;

	public UnitOfWork(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task SaveChangesAsync(CancellationToken cancellationToken)
	{
		await _dbContext.SaveChangesAsync(cancellationToken);
	}
}