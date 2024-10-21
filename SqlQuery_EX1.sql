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

INSERT INTO Keywords (KeywordID, Keyword) VALUES (1, 'Blue');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (2, 'Yellow');
INSERT INTO Keywords (KeywordID, Keyword) VALUES (3, 'Green');

INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (1, 1);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (1, 2);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (2, 1);
INSERT INTO DocumentKeywords (DocID, KeywordID) VALUES (3, 2);
