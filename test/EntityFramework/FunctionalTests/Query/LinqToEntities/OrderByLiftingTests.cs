﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Query.LinqToEntities
{
    using System.Data.Entity.TestModels.ArubaModel;
    using SimpleModel;
    using System.Data.Entity.Infrastructure;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class OrderByLiftingTests : FunctionalTestBase
    {
        [Fact]
        public void OrderBy_ThenBy_lifted_above_projection()
        {
            using (var context = new ArubaContext())
            {
                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 });
                var baseline = context.Owners.Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_lifted_above_filter_with_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = true;

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Where(p => p.Id % 2 == 0);
                var baseline = context.Owners.Where(p => p.Id % 2 == 0).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_lifted_above_filter_without_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = false;

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Where(p => p.Id % 2 == 0);
                var baseline = context.Owners.Where(p => p.Id % 2 == 0).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_lifted_above_type_filter()
        {
            using (var context = new ArubaContext())
            {
                var query = context.Configs.OrderByDescending(p => p.Arch).ThenBy(p => p.Id).OfType<ArubaMachineConfig>();
                var baseline = context.Configs.OfType<ArubaMachineConfig>().OrderByDescending(p => p.Arch).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Configs.ToList().OrderByDescending(p => p.Arch).ThenBy(p => p.Id).OfType<ArubaMachineConfig>().ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_lifted_above_projection()
        {
            using (var context = new ArubaContext())
            {
                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 });
                var baseline = context.Owners.Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_lifted_above_filter_with_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = true;

                var expectedSq =
@"SELECT 
[Skip1].[Id] AS [Id], 
[Skip1].[FirstName] AS [FirstName], 
[Skip1].[LastName] AS [LastName], 
[Skip1].[Alias] AS [Alias]
FROM ( SELECT [Extent1].[Id] AS [Id], [Extent1].[FirstName] AS [FirstName], [Extent1].[LastName] AS [LastName], [Extent1].[Alias] AS [Alias]
	FROM ( SELECT [Extent1].[Id] AS [Id], [Extent1].[FirstName] AS [FirstName], [Extent1].[LastName] AS [LastName], [Extent1].[Alias] AS [Alias], row_number() OVER (ORDER BY [Extent1].[FirstName] DESC, [Extent1].[Id] ASC) AS [row_number]
		FROM [dbo].[ArubaOwners] AS [Extent1]
	)  AS [Extent1]
	WHERE [Extent1].[row_number] > 5
)  AS [Skip1]
WHERE (0 = ([Skip1].[Id] % 2)) AND ([Skip1].[Id] % 2 IS NOT NULL)
ORDER BY [Skip1].[FirstName] DESC, [Skip1].[Id] ASC";

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Where(p => p.Id % 2 == 0);
                QueryTestHelpers.VerifyDbQuery(query, expectedSq);

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_lifted_above_filter_without_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = false;

                var expectedSq =
@"SELECT 
[Skip1].[Id] AS [Id], 
[Skip1].[FirstName] AS [FirstName], 
[Skip1].[LastName] AS [LastName], 
[Skip1].[Alias] AS [Alias]
FROM ( SELECT [Extent1].[Id] AS [Id], [Extent1].[FirstName] AS [FirstName], [Extent1].[LastName] AS [LastName], [Extent1].[Alias] AS [Alias]
	FROM ( SELECT [Extent1].[Id] AS [Id], [Extent1].[FirstName] AS [FirstName], [Extent1].[LastName] AS [LastName], [Extent1].[Alias] AS [Alias], row_number() OVER (ORDER BY [Extent1].[FirstName] DESC, [Extent1].[Id] ASC) AS [row_number]
		FROM [dbo].[ArubaOwners] AS [Extent1]
	)  AS [Extent1]
	WHERE [Extent1].[row_number] > 5
)  AS [Skip1]
WHERE 0 = ([Skip1].[Id] % 2)
ORDER BY [Skip1].[FirstName] DESC, [Skip1].[Id] ASC";

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Where(p => p.Id % 2 == 0);
                QueryTestHelpers.VerifyDbQuery(query, expectedSq);

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_lifted_above_type_filter()
        {
            using (var context = new ArubaContext())
            {
                var expectedSql =
@"SELECT 
[Project1].[C1] AS [C1], 
[Project1].[C2] AS [C2], 
[Project1].[C3] AS [C3], 
[Project1].[C4] AS [C4], 
[Project1].[C5] AS [C5], 
[Project1].[C6] AS [C6], 
[Project1].[C7] AS [C7], 
[Project1].[C8] AS [C8]
FROM ( SELECT 
	[Skip1].[Id] AS [Id], 
	[Skip1].[Arch] AS [Arch], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[Discriminator] END AS [C1], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[Id] END AS [C2], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[OS] END AS [C3], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[Lang] END AS [C4], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[Arch] END AS [C5], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[Host] END AS [C6], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[Address] END AS [C7], 
	CASE WHEN ([Skip1].[Discriminator] = N'ArubaMachineConfig') THEN [Skip1].[Location] END AS [C8]
	FROM ( SELECT [Filter1].[Id] AS [Id], [Filter1].[OS] AS [OS], [Filter1].[Lang] AS [Lang], [Filter1].[Arch] AS [Arch], [Filter1].[Host] AS [Host], [Filter1].[Address] AS [Address], [Filter1].[Location] AS [Location], [Filter1].[Discriminator] AS [Discriminator]
		FROM ( SELECT [Extent1].[Id] AS [Id], [Extent1].[OS] AS [OS], [Extent1].[Lang] AS [Lang], [Extent1].[Arch] AS [Arch], [Extent1].[Host] AS [Host], [Extent1].[Address] AS [Address], [Extent1].[Location] AS [Location], [Extent1].[Discriminator] AS [Discriminator], row_number() OVER (ORDER BY [Extent1].[Arch] DESC, [Extent1].[Id] ASC) AS [row_number]
			FROM [dbo].[ArubaConfigs] AS [Extent1]
			WHERE [Extent1].[Discriminator] IN (N'ArubaMachineConfig',N'ArubaConfig')
		)  AS [Filter1]
		WHERE [Filter1].[row_number] > 5
	)  AS [Skip1]
	WHERE [Skip1].[Discriminator] = N'ArubaMachineConfig'
)  AS [Project1]
ORDER BY [Project1].[Arch] DESC, [Project1].[Id] ASC";
                var query = context.Configs.OrderByDescending(p => p.Arch).ThenBy(p => p.Id).Skip(5).OfType<ArubaMachineConfig>();
                QueryTestHelpers.VerifyDbQuery(query, expectedSql);

                var results = query.ToList();
                var expected = context.Configs.ToList().OrderByDescending(p => p.Arch).ThenBy(p => p.Id).Skip(5).OfType<ArubaMachineConfig>().ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Take_lifted_above_projection()
        {
            using (var context = new ArubaContext())
            {
                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 });
                var baseline = context.Owners.Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);

            }
        }

        [Fact]
        public void OrderBy_ThenBy_Take_lifted_above_filter_with_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = true;

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Where(p => p.Id % 2 == 0);
                var baseline = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Where(p => p.Id % 2 == 0).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Take_lifted_above_filter_without_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = false;

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Where(p => p.Id % 2 == 0);
                var baseline = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Where(p => p.Id % 2 == 0).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Take(10).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Take_lifted_above_type_filter()
        {
            using (var context = new ArubaContext())
            {
                var expectedSql =
@"SELECT 
[Project1].[C1] AS [C1], 
[Project1].[C2] AS [C2], 
[Project1].[C3] AS [C3], 
[Project1].[C4] AS [C4], 
[Project1].[C5] AS [C5], 
[Project1].[C6] AS [C6], 
[Project1].[C7] AS [C7], 
[Project1].[C8] AS [C8]
FROM ( SELECT 
	[Limit1].[Id] AS [Id], 
	[Limit1].[Arch] AS [Arch], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Discriminator] END AS [C1], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Id] END AS [C2], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[OS] END AS [C3], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Lang] END AS [C4], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Arch] END AS [C5], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Host] END AS [C6], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Address] END AS [C7], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Location] END AS [C8]
	FROM ( SELECT TOP (10) [Extent1].[Id] AS [Id], [Extent1].[OS] AS [OS], [Extent1].[Lang] AS [Lang], [Extent1].[Arch] AS [Arch], [Extent1].[Host] AS [Host], [Extent1].[Address] AS [Address], [Extent1].[Location] AS [Location], [Extent1].[Discriminator] AS [Discriminator]
		FROM [dbo].[ArubaConfigs] AS [Extent1]
		WHERE [Extent1].[Discriminator] IN (N'ArubaMachineConfig',N'ArubaConfig')
		ORDER BY [Extent1].[Arch] DESC, [Extent1].[Id] ASC
	)  AS [Limit1]
	WHERE [Limit1].[Discriminator] = N'ArubaMachineConfig'
)  AS [Project1]
ORDER BY [Project1].[Arch] DESC, [Project1].[Id] ASC";
                
                var query = context.Configs.OrderByDescending(p => p.Arch).ThenBy(p => p.Id).Take(10).OfType<ArubaMachineConfig>();
                QueryTestHelpers.VerifyDbQuery(query, expectedSql);

                var results = query.ToList();
                var expected = context.Configs.ToList().OrderByDescending(p => p.Arch).ThenBy(p => p.Id).Take(10).OfType<ArubaMachineConfig>().ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_Take_lifted_above_projection()
        {
            using (var context = new ArubaContext())
            {
                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 });
                var baseline = context.Owners.Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Select(p => new { p.FirstName, p.Id, Foo = p.Id * 5 }).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_Take_lifted_above_filter_with_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = true;

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Where(p => p.Id % 2 == 0);
                var baseline = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Where(p => p.Id % 2 == 0).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_Take_lifted_above_filter_without_clr_null_semantics()
        {
            using (var context = new ArubaContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.ContextOptions.UseCSharpNullComparisonBehavior = false;

                var query = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Where(p => p.Id % 2 == 0);
                var baseline = context.Owners.OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Where(p => p.Id % 2 == 0).OrderByDescending(p => p.FirstName).ThenBy(p => p.Id);
                Assert.Equal(baseline.ToString(), query.ToString());

                var results = query.ToList();
                var expected = context.Owners.ToList().OrderByDescending(p => p.FirstName).ThenBy(p => p.Id).Skip(5).Take(10).Where(p => p.Id % 2 == 0).ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }

        [Fact]
        public void OrderBy_ThenBy_Skip_Take_lifted_above_type_filter()
        {
            using (var context = new ArubaContext())
            {
                var expectedSql =
@"SELECT 
[Project1].[C1] AS [C1], 
[Project1].[C2] AS [C2], 
[Project1].[C3] AS [C3], 
[Project1].[C4] AS [C4], 
[Project1].[C5] AS [C5], 
[Project1].[C6] AS [C6], 
[Project1].[C7] AS [C7], 
[Project1].[C8] AS [C8]
FROM ( SELECT 
	[Limit1].[Id] AS [Id], 
	[Limit1].[Arch] AS [Arch], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Discriminator] END AS [C1], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Id] END AS [C2], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[OS] END AS [C3], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Lang] END AS [C4], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Arch] END AS [C5], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Host] END AS [C6], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Address] END AS [C7], 
	CASE WHEN ([Limit1].[Discriminator] = N'ArubaMachineConfig') THEN [Limit1].[Location] END AS [C8]
	FROM ( SELECT TOP (10) [Filter1].[Id] AS [Id], [Filter1].[OS] AS [OS], [Filter1].[Lang] AS [Lang], [Filter1].[Arch] AS [Arch], [Filter1].[Host] AS [Host], [Filter1].[Address] AS [Address], [Filter1].[Location] AS [Location], [Filter1].[Discriminator] AS [Discriminator]
		FROM ( SELECT [Extent1].[Id] AS [Id], [Extent1].[OS] AS [OS], [Extent1].[Lang] AS [Lang], [Extent1].[Arch] AS [Arch], [Extent1].[Host] AS [Host], [Extent1].[Address] AS [Address], [Extent1].[Location] AS [Location], [Extent1].[Discriminator] AS [Discriminator], row_number() OVER (ORDER BY [Extent1].[Arch] DESC, [Extent1].[Id] ASC) AS [row_number]
			FROM [dbo].[ArubaConfigs] AS [Extent1]
			WHERE [Extent1].[Discriminator] IN (N'ArubaMachineConfig',N'ArubaConfig')
		)  AS [Filter1]
		WHERE [Filter1].[row_number] > 5
		ORDER BY [Filter1].[Arch] DESC, [Filter1].[Id] ASC
	)  AS [Limit1]
	WHERE [Limit1].[Discriminator] = N'ArubaMachineConfig'
)  AS [Project1]
ORDER BY [Project1].[Arch] DESC, [Project1].[Id] ASC";

                var query = context.Configs.OrderByDescending(p => p.Arch).ThenBy(p => p.Id).Skip(5).Take(10).OfType<ArubaMachineConfig>();
                QueryTestHelpers.VerifyDbQuery(query, expectedSql);

                var results = query.ToList();
                var expected = context.Configs.ToList().OrderByDescending(p => p.Arch).ThenBy(p => p.Id).Skip(5).Take(10).OfType<ArubaMachineConfig>().ToList();
                QueryTestHelpers.VerifyQueryResult(expected, results, (o, i) => o.Id == i.Id);
            }
        }
    }
}
