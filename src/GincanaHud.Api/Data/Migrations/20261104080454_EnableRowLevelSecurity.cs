using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GincanaHud.Api.Data.Migrations
{
    /// <summary>
    /// Supabase serves every table in <c>public</c> through its Data API to the <c>anon</c> and
    /// <c>authenticated</c> roles. RLS with no policies shuts them out; the Api connects as the tables'
    /// owner, which RLS doesn't apply to (D29).
    /// </summary>
    public partial class EnableRowLevelSecurity : Migration
    {
        private static readonly string[] Tables =
        [
            "Organizations", "Users", "Activities", "AdminUsers", "Pois",
            "ActivityParticipants", "ActivityPois", "Captures", "__EFMigrationsHistory",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"""ALTER TABLE "{table}" ENABLE ROW LEVEL SECURITY;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"""ALTER TABLE "{table}" DISABLE ROW LEVEL SECURITY;""");
        }
    }
}
