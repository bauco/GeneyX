using System;
using System.Collections.Generic;
using System.Data.SqlClient;


public class Ex1
{
	public Ex1()
	{
        public class Document
        {
            public int ID { get; set; }
            public DateTime Date { get; set; }
        }
        public class Keyword
        {
            public int ID { get; set; }
            public string Keyword { get; set; }
        }
        public class DocumentKeyword
        {
            public int DocumentID { get; set; }
            public int KeywordID { get; set; }
        }
        
        public class DocumentRepository
        {
            private string connectionString;

            public DocumentRepository(string connectionString)
            {
                this.connectionString = connectionString;
            }

            private List<Document> ExecuteQuery(string query, SqlParameter[] parameters = null)
            {
                List<Document> documents = new List<Document>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);

                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Document doc = new Document
                        {
                            DocID = reader.GetInt32(0),
                            DocDate = reader.GetDateTime(1)
                        };
                        documents.Add(doc);
                    }
                }

                return documents;
            }

            // Part 1: Documents with a DocDate after 4/1/1995
            public List<Document> GetDocumentsAfterDate(DateTime date)
            {
                string query = "SELECT DocID, DocDate FROM Documents WHERE DocDate > @DocDate";
                SqlParameter[] parameters = { new SqlParameter("@DocDate", date) };

                return ExecuteQuery(query, parameters);
            }

            // Part 2: Documents that contain the keyword "Blue"
            public List<Document> GetDocumentsByKeyword(string keyword)
            {
                string query = @"SELECT d.DocID, d.DocDate 
                             FROM Documents d
                             JOIN DocumentKeywords dk ON d.DocID = dk.DocID
                             JOIN Keywords k ON dk.KeywordID = k.KeywordID
                             WHERE k.Keyword = @Keyword";
                SqlParameter[] parameters = { new SqlParameter("@Keyword", keyword) };

                return ExecuteQuery(query, parameters);
            }

            // Part 3: Documents that contain either the keyword "Blue" or "Yellow"
            public List<Document> GetDocumentsByKeywords(List<string> keywords)
            {
                string keywordList = string.Join(",", keywords.ConvertAll(k => $"'{k}'"));
                string query = @$"SELECT DISTINCT d.DocID, d.DocDate
                              FROM Documents d
                              JOIN DocumentKeywords dk ON d.DocID = dk.DocID
                              JOIN Keywords k ON dk.KeywordID = k.KeywordID
                              WHERE k.Keyword IN ({keywordList})";

                return ExecuteQuery(query);
            }

            // Part 4: Documents that contain both the keywords "Blue" and "Yellow"
            public List<Document> GetDocumentsByMultipleKeywords(List<string> keywords)
            {
                string keywordList = string.Join(",", keywords.ConvertAll(k => $"'{k}'"));
                string query = @$"SELECT d.DocID, d.DocDate
                              FROM Documents d
                              JOIN DocumentKeywords dk ON d.DocID = dk.DocID
                              JOIN Keywords k ON dk.KeywordID = k.KeywordID
                              WHERE k.Keyword IN ({keywordList})
                              GROUP BY d.DocID, d.DocDate
                              HAVING COUNT(DISTINCT k.Keyword) = @KeywordCount";
                SqlParameter[] parameters = { new SqlParameter("@KeywordCount", keywords.Count) };

                return ExecuteQuery(query, parameters);
            }
        }
    }
}
