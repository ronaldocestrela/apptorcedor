using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Persistence;

/// <summary>Detecta conflito de índice único (incl. 2601/2627 do SQL Server).</summary>
public static class SqlServerUniqueIndexViolation
{
    public static bool IsUniqueConstraintOnSave(DbUpdateException ex) =>
        FindSqlException(ex) is { Number: 2601 or 2627 };

    private static SqlException? FindSqlException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is SqlException se)
                return se;
            ex = ex.InnerException;
        }

        return null;
    }
}
