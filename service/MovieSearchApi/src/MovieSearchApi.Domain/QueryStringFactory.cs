﻿using System.Collections.Generic;
using System.Linq;
using MovieSearchApi.Common;
using Nest;

namespace MovieSearchApi.Domain
{
    public static class QueryStringFactory
    {
        public static QueryContainer CreateQueryString(SearchRequest searchRequest)
        {
            var queryContainer = new QueryContainer();

            foreach (var indexField in ElasticsearchMovieFieldHelper.AllIndexFields)
            {
                foreach (var indexAnalyzer in ElasticsearchMovieAnalyzerHelper.AllIndexAnalyzers)
                {
                    if (ShouldSearchIndex(indexAnalyzer, searchRequest.SearchSettings))
                    {
                        queryContainer |=
                            new QueryContainerDescriptor<Movie>()
                                .MatchPhrase(mqd => mqd
                                    .Field(indexField.AppendSuffix(indexAnalyzer))
                                    .Analyzer(indexAnalyzer.ToSearchAnalyzer())
                                    .Boost(GetAnalyzerBoost(indexAnalyzer))
                                    .Slop(50)
                                    .Query(searchRequest.Query));
                    }
                }
            }

            foreach (var indexAnalyzer in ElasticsearchMovieAnalyzerHelper.AllIndexAnalyzers)
            {
                queryContainer |= new QueryContainerDescriptor<Movie>()
                    .MultiMatch(mmqd => mmqd
                        .Analyzer(indexAnalyzer)
                        .Type(TextQueryType.CrossFields)
                        .Boost(0.1)
                        .Query(searchRequest.Query)
                        .Operator(Operator.And)
                        .Fields(fd =>
                        {
                            foreach (var indexField in ElasticsearchMovieFieldHelper.AllIndexFields)
                            {
                                fd.Field(indexField.AppendSuffix(indexAnalyzer));
                            }

                            return fd;
                        }));
            }

            return queryContainer;
        }

        private static double GetAnalyzerBoost(string analyzer)
        {
            switch (analyzer)
            {
                case ElasticsearchMovieAnalyzerHelper.Standard:
                    return 1.0;
                case ElasticsearchMovieAnalyzerHelper.Snowball:
                    return 0.5;
                default:
                    return 1;
            }
        }

        private static bool ShouldSearchIndex(string indexAnalyzer, SearchSettings searchSettings)
        {
            // Todo: refactor this
            if (indexAnalyzer == ElasticsearchMovieAnalyzerHelper.Standard && searchSettings.StandardAnalyzer)
            {
                return true;
            }

            if (indexAnalyzer == ElasticsearchMovieAnalyzerHelper.Snowball && searchSettings.SnowballAnalyzer)
            {
                return true;
            }

            if (indexAnalyzer == ElasticsearchMovieAnalyzerHelper.EdgeNGram && searchSettings.EdgeNGramAnalyzer)
            {
                return true;
            }

            return false;
        }
    }
}