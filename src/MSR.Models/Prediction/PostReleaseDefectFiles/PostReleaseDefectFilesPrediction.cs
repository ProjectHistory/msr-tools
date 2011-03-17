/*
 * MSR Tools - tools for mining software repositories
 * 
 * Copyright (C) 2011  Semyon Kirnosenko
 */

using System;
using System.Collections.Generic;
using System.Linq;

using MSR.Data;
using MSR.Data.Entities;
using MSR.Data.Entities.DSL.Selection;
using MSR.Data.Entities.DSL.Selection.Metrics;
using MSR.Models.Regressions;

namespace MSR.Models.Prediction.PostReleaseDefectFiles
{
	public class PostReleaseDefectFilesPrediction : Prediction
	{
		public PostReleaseDefectFilesPrediction(IRepositoryResolver repositories)
			: base(repositories)
		{
			FilePortionLimit = 0.2;
		}
		public virtual IEnumerable<string> Predict(IEnumerable<string> releases)
		{
			IEnumerable<string> previousReleaseRevisions = releases.Take(releases.Count() - 1);
			string releaseRevision = releases.Last();
			
			LogisticRegression lr = new LogisticRegression();
			
			string previousRevision = null;
			foreach (var revision in previousReleaseRevisions)
			{
				foreach (var file in FilesInRevision(revision))
				{
					context
						.SetCommits(previousRevision, revision)
						.SetFiles(e => e.IdIs(file.ID));
					
					lr.AddTrainingData(
						GetPredictorValuesFor(context),
						FileHasDefects(file.ID, revision, previousRevision)
					);
				}
				previousRevision = revision;
			}
			
			lr.Train();

			var files = FilesInRevision(releaseRevision);
			int filesInRelease = files.Count();
			
			context.SetCommits(previousReleaseRevisions.Last(), releaseRevision);
			
			var faultProneFiles =
				(
					from f in files
					select new
					{
						Path = f.Path,
						FaultProneProbability = lr.Predict(
							GetPredictorValuesFor(context.SetFiles(e => e.IdIs(f.ID)))
						)
					}
				).Where(x => x.FaultProneProbability > 0.5)
				.OrderByDescending(x => x.FaultProneProbability);

			return faultProneFiles
				.Select(x => x.Path)
				.TakeNoMoreThan((int)(filesInRelease * FilePortionLimit));
		}
		public Func<ProjectFileSelectionExpression,ProjectFileSelectionExpression> FileSelector
		{
			get; set;
		}
		public double FilePortionLimit
		{
			get; set;
		}
		protected IEnumerable<ProjectFile> FilesInRevision(string revision)
		{
			return repositories.SelectionDSL()
				.Files()
					.Reselect(FileSelector)
					.ExistInRevision(revision)
					.ToList();
		}
		protected double FileHasDefects(int fileID, string revision, string previousRevision)
		{
			return repositories.SelectionDSL()
				.Files().IdIs(fileID)
				.Commits()
					.AfterRevision(previousRevision)
					.TillRevision(revision)
				.Modifications().InCommits().InFiles()
				.CodeBlocks().InModifications().CalculateNumberOfDefects() > 0 ? 1 : 0;
		}
	}
}
