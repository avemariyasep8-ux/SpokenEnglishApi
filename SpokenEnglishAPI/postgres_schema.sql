-- PostgreSQL Schema for SpokenEnglish
-- Run this on your Supabase or local PostgreSQL database

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- =============================================
-- TABLES
-- =============================================

CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    userguid UUID NOT NULL DEFAULT gen_random_uuid(),
    email VARCHAR(200) NOT NULL,
    passwordhash VARCHAR(500) NOT NULL,
    apikey VARCHAR(200) NOT NULL,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INT NULL,
    createddate TIMESTAMP NOT NULL DEFAULT NOW(),
    modifyby INT NULL,
    modifydate TIMESTAMP NULL,
    mobilenumber VARCHAR(15) NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'User',
    mobile VARCHAR(15) NULL,
    refreshtoken VARCHAR(200) NULL,
    refreshtokenexpiry TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS appuser (
    userid SERIAL PRIMARY KEY,
    username VARCHAR(100) NULL,
    mobileno VARCHAR(20) NULL,
    createddate TIMESTAMP NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS lessontype (
    lessontypeid SERIAL PRIMARY KEY,
    typename VARCHAR(50) NULL
);

CREATE TABLE IF NOT EXISTS lesson (
    lessonid SERIAL PRIMARY KEY,
    lessontypeid INT NULL REFERENCES lessontype(lessontypeid),
    lessonorder INT NULL,
    isactive BOOLEAN NULL
);

CREATE TABLE IF NOT EXISTS language (
    languageid SERIAL PRIMARY KEY,
    languagename VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS lesson_lang (
    lessonlangid SERIAL PRIMARY KEY,
    lessonid INT NOT NULL REFERENCES lesson(lessonid) ON DELETE CASCADE,
    languageid INT NOT NULL,
    lessonname VARCHAR(200) NOT NULL,
    description VARCHAR(500) NULL,
    createddate TIMESTAMP NOT NULL DEFAULT NOW(),
    updateddate TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS meaningquestion (
    questionid SERIAL PRIMARY KEY,
    lessonid INT NULL REFERENCES lesson(lessonid)
);

CREATE TABLE IF NOT EXISTS meaningquestion_lang (
    id SERIAL PRIMARY KEY,
    questionid INT NOT NULL REFERENCES meaningquestion(questionid),
    languageid INT NOT NULL,
    questiontext VARCHAR(300) NULL
);

CREATE TABLE IF NOT EXISTS meaningoption (
    optionid SERIAL PRIMARY KEY,
    questionid INT NULL REFERENCES meaningquestion(questionid),
    iscorrect BOOLEAN NULL
);

CREATE TABLE IF NOT EXISTS meaningoption_lang (
    id SERIAL PRIMARY KEY,
    optionid INT NOT NULL REFERENCES meaningoption(optionid),
    languageid INT NOT NULL,
    optiontext VARCHAR(200) NULL
);

CREATE TABLE IF NOT EXISTS arrangesentence (
    arrangesentenceid SERIAL PRIMARY KEY,
    lessonid INT NULL REFERENCES lesson(lessonid)
);

CREATE TABLE IF NOT EXISTS arrangesentence_lang (
    id SERIAL PRIMARY KEY,
    arrangesentenceid INT NOT NULL REFERENCES arrangesentence(arrangesentenceid),
    languageid INT NOT NULL,
    correctsentence VARCHAR(500) NULL
);

CREATE TABLE IF NOT EXISTS arrangesentenceword (
    wordid SERIAL PRIMARY KEY,
    arrangesentenceid INT NOT NULL REFERENCES arrangesentence(arrangesentenceid),
    correctorder INT NOT NULL
);

CREATE TABLE IF NOT EXISTS arrangesentenceword_lang (
    id SERIAL PRIMARY KEY,
    wordid INT NOT NULL REFERENCES arrangesentenceword(wordid),
    languageid INT NOT NULL,
    wordtext VARCHAR(100) NULL
);

CREATE TABLE IF NOT EXISTS readingsentence (
    readingsentenceid SERIAL PRIMARY KEY,
    lessonid INT NULL REFERENCES lesson(lessonid)
);

CREATE TABLE IF NOT EXISTS readingsentence_lang (
    id SERIAL PRIMARY KEY,
    readingsentenceid INT NOT NULL REFERENCES readingsentence(readingsentenceid),
    languageid INT NOT NULL,
    sentencetext VARCHAR(500) NULL,
    referenceaudiourl VARCHAR(300) NULL
);

CREATE TABLE IF NOT EXISTS useranswer (
    answerid SERIAL PRIMARY KEY,
    userid INT NULL REFERENCES appuser(userid),
    lessonid INT NULL REFERENCES lesson(lessonid),
    activitytype VARCHAR(50) NULL,
    referenceid INT NULL,
    iscorrect BOOLEAN NULL,
    attemptdate TIMESTAMP NULL DEFAULT NOW(),
    languageid INT NULL
);

CREATE TABLE IF NOT EXISTS refreshtokens (
    refreshtokenid SERIAL PRIMARY KEY,
    userid INT NOT NULL UNIQUE,
    token VARCHAR(500) NOT NULL,
    expiresat TIMESTAMP NOT NULL,
    isrevoked BOOLEAN NULL DEFAULT false,
    createddate TIMESTAMP NULL DEFAULT NOW(),
    expirydate TIMESTAMP NOT NULL DEFAULT NOW() + INTERVAL '7 days'
);

CREATE TABLE IF NOT EXISTS userrefreshtoken (
    refreshtokenid SERIAL PRIMARY KEY,
    userid INT NOT NULL REFERENCES users(id),
    refreshtoken VARCHAR(500) NOT NULL,
    expirydate TIMESTAMP NOT NULL,
    isrevoked BOOLEAN NOT NULL DEFAULT false,
    createddate TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS userotp (
    id SERIAL PRIMARY KEY,
    userid INT NULL,
    otp VARCHAR(6) NULL,
    expiresat TIMESTAMP NULL,
    isused BOOLEAN NULL DEFAULT false
);

CREATE TABLE IF NOT EXISTS passwordresettokens (
    id SERIAL PRIMARY KEY,
    userid INT NULL,
    token VARCHAR(100) NULL,
    expiresat TIMESTAMP NULL,
    isused BOOLEAN NULL DEFAULT false
);

CREATE TABLE IF NOT EXISTS apiusage (
    id SERIAL PRIMARY KEY,
    userguid UUID NOT NULL,
    endpoint VARCHAR(100) NOT NULL,
    requestcount INT NOT NULL DEFAULT 1,
    usagedate DATE NOT NULL DEFAULT CURRENT_DATE,
    UNIQUE (userguid, endpoint, usagedate)
);

-- =============================================
-- SEED DATA (optional starter data)
-- =============================================

INSERT INTO language (languagename) VALUES ('English'), ('Hindi') ON CONFLICT DO NOTHING;
INSERT INTO lessontype (typename) VALUES ('Vocabulary'), ('Grammar'), ('Reading'), ('Listening') ON CONFLICT DO NOTHING;
