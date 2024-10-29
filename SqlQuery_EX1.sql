-- Creating the tables for Documents, Keywords, and DocumentKeywords
CREATE TABLE Documents (
    DocID INT PRIMARY KEY,
    DocDate DATE
);

CREATE TABLE Keywords (
    KeywordID INT PRIMARY KEY,
    Keyword VARCHAR(100)
);

CREATE TABLE DocumentKeywords (
    DocID INT,
    KeywordID INT,
    PRIMARY KEY (DocID, KeywordID),
    FOREIGN KEY (DocID) REFERENCES Documents(DocID),
    FOREIGN KEY (KeywordID) REFERENCES Keywords(KeywordID)
);

-- Inserting sample data into the tables
INSERT INTO Documents (DocID, DocDate) VALUES (1, '1996-05-20');
INSERT INTO Documents (DocID, DocDate) VALUES (2, '1994-07-15');
INSERT INTO Documents (DocID, DocDate) VALUES (3, '1997-11-30');
INSERT INTO Documents (DocID, DocDate) VALUES (4, '1999-02-13');
INSERT INTO Documents (DocID, DocDate) VALUES (5, '2002-04-17');
INSERT INTO Documents (DocID, DocDate) VALUES (6, '2005-08-29');
INSERT INTO Documents (DocID, DocDate) VALUES (7, '2010-12-03');
INSERT INTO Documents (DocID, DocDate) VALUES (8, '2015-09-21');
INSERT INTO Documents (DocID, DocDate) VALUES (9, '2018-03-11');
INSERT INTO Documents (DocID, DocDate) VALUES (10, '2020-07-19');

INSERT INTO Keywords (KeywordID, Keyword) VALUES (1, 'Blue');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (2, 'Yellow');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (3, 'Green');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (4, 'Red');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (5, 'Orange');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (6, 'Purple');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (7, 'White');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (8, 'Black');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (9, 'Pink');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (10, 'Violet');

INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (1, 1);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (1, 2);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (1, 3);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (2, 1);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (2, 2);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (3, 2);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (3, 3);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (4, 4);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (4, 1);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (5, 5);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (5, 6);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (6, 7);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (6, 8);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (7, 9);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (7, 10)
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (8, 2);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (8, 3);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (9, 4);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (9, 6);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (10, 1)
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (10, 9)

SELECT * 
FROM Documents
WHERE DocDate > '1995-04-01';


SELECT d.*
FROM Documents d
JOIN DocumentKeywords dk ON d.DocID = dk.DocID
JOIN Keywords k ON dk.KeywordID = k.KeywordID
WHERE k.Keyword = 'Blue';

SELECT DISTINCT d.*
FROM Documents d
JOIN DocumentKeywords dk ON d.DocID = dk.DocID
JOIN Keywords k ON dk.KeywordID = k.KeywordID
WHERE k.Keyword IN ('Blue', 'Yellow');

SELECT d.*
FROM Documents d
WHERE d.DocID IN (
    SELECT dk1.DocID
    FROM DocumentKeywords dk1
    JOIN Keywords k1 ON dk1.KeywordID = k1.KeywordID
    WHERE k1.Keyword = 'Blue'
)
AND d.DocID IN (
    SELECT dk2.DocID
    FROM DocumentKeywords dk2
    JOIN Keywords k2 ON dk2.KeywordID = k2.KeywordID
    WHERE k2.Keyword = 'Yellow'
);

------- OPTIMIZED QUERIES -------

CREATE INDEX idx_docdate ON Documents(DocDate);

SELECT d.DocID, d.DocDate
FROM Documents d
JOIN DocumentKeywords dk ON d.DocID = dk.DocID
JOIN Keywords k ON dk.KeywordID = k.KeywordID
WHERE k.KeyWord = 'Blue';

CREATE INDEX idx_documentkeywords_docid ON DocumentKeywords(DocID);
CREATE INDEX idx_documentkeywords_keywordid ON DocumentKeywords(KeyWordID);

SELECT DISTINCT d.DocID, d.DocDate
FROM Documents d
JOIN DocumentKeywords dk ON d.DocID = dk.DocID
JOIN Keywords k ON dk.KeywordID = k.KeywordID
WHERE k.KeyWord IN ('Blue', 'Yellow');

SELECT d.DocID, d.DocDate
FROM Documents d
JOIN DocumentKeywords dk ON d.DocID = dk.DocID
JOIN Keywords k ON dk.KeywordID = k.KeywordID
WHERE k.KeyWord IN ('Blue', 'Yellow')
GROUP BY d.DocID, d.DocDate
HAVING COUNT(DISTINCT k.KeyWord) = 2;

---- C# Code in ex1.cs ----

