-- PostgreSQL Functions (equivalent to SQL Server Stored Procedures)
-- Run this after postgres_schema.sql

-- =============================================
-- USER FUNCTIONS
-- =============================================

CREATE OR REPLACE FUNCTION sp_user_login(p_logininput VARCHAR)
RETURNS TABLE (
    id INT, userguid UUID, email VARCHAR, mobilenumber VARCHAR,
    passwordhash VARCHAR, apikey VARCHAR, isactive BOOLEAN, role VARCHAR
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.userguid, u.email, u.mobilenumber,
           u.passwordhash, u.apikey, u.isactive, u.role
    FROM users u
    WHERE u.isactive = true
      AND (u.email = p_logininput OR u.mobilenumber = p_logininput)
    LIMIT 1;
END;
$$;

CREATE OR REPLACE FUNCTION sp_validate_apikey(p_apikey VARCHAR)
RETURNS UUID LANGUAGE plpgsql AS $$
DECLARE v_guid UUID;
BEGIN
    SELECT userguid INTO v_guid FROM users WHERE apikey = p_apikey AND isactive = true;
    RETURN v_guid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_user_create(p_email VARCHAR, p_passwordhash VARCHAR, p_apikey VARCHAR)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM users WHERE email = p_email) THEN
        RAISE EXCEPTION 'Email already exists';
    END IF;
    INSERT INTO users (userguid, email, passwordhash, apikey, isactive)
    VALUES (gen_random_uuid(), p_email, p_passwordhash, p_apikey, true);
END;
$$;

CREATE OR REPLACE FUNCTION sp_save_refreshtoken(p_userid INT, p_token VARCHAR, p_expiresat TIMESTAMPTZ)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO refreshtokens (userid, token, expiresat, expirydate, isrevoked, createddate)
    VALUES (p_userid, p_token, p_expiresat, p_expiresat, false, NOW())
    ON CONFLICT (userid) DO UPDATE
    SET token = EXCLUDED.token, expiresat = EXCLUDED.expiresat,
        expirydate = EXCLUDED.expirydate, isrevoked = false, createddate = NOW();
END;
$$;

CREATE OR REPLACE FUNCTION sp_validate_refreshtoken(p_token VARCHAR)
RETURNS TABLE (
    id INT, userguid UUID, email VARCHAR, mobilenumber VARCHAR,
    passwordhash VARCHAR, apikey VARCHAR, isactive BOOLEAN, role VARCHAR
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.userguid, u.email, u.mobilenumber,
           u.passwordhash, u.apikey, u.isactive, u.role
    FROM refreshtokens rt
    JOIN users u ON u.id = rt.userid
    WHERE rt.token = p_token AND rt.isrevoked = false AND rt.expirydate > NOW();
END;
$$;

-- =============================================
-- LESSON FUNCTIONS
-- =============================================

CREATE OR REPLACE FUNCTION sp_lesson_get(p_languageid INT)
RETURNS TABLE (
    lessonid INT, lessontype VARCHAR, lessonname VARCHAR,
    description VARCHAR, lessonorder INT
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT l.lessonid, lt.typename, ll.lessonname, ll.description, l.lessonorder
    FROM lesson l
    JOIN lessontype lt ON l.lessontypeid = lt.lessontypeid
    JOIN lesson_lang ll ON l.lessonid = ll.lessonid
    WHERE ll.languageid = p_languageid AND l.isactive = true
    ORDER BY l.lessonorder;
END;
$$;

CREATE OR REPLACE FUNCTION sp_lesson_getdetail(p_lessonid INT)
RETURNS TABLE (
    lessonid INT, lessontypeid INT, lessonorder INT, isactive BOOLEAN,
    languageid INT, lessonname VARCHAR, description VARCHAR
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT l.lessonid, l.lessontypeid, l.lessonorder, l.isactive,
           ll.languageid, ll.lessonname, ll.description
    FROM lesson l
    JOIN lesson_lang ll ON l.lessonid = ll.lessonid
    WHERE l.lessonid = p_lessonid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_lesson_add(
    p_lessontypeid INT, p_lessonorder INT,
    p_languageid INT, p_lessonname VARCHAR, p_description VARCHAR
) RETURNS INT LANGUAGE plpgsql AS $$
DECLARE v_lessonid INT;
BEGIN
    INSERT INTO lesson (lessontypeid, lessonorder, isactive)
    VALUES (p_lessontypeid, p_lessonorder, true)
    RETURNING lessonid INTO v_lessonid;

    INSERT INTO lesson_lang (lessonid, languageid, lessonname, description)
    VALUES (v_lessonid, p_languageid, p_lessonname, p_description);

    RETURN v_lessonid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_lesson_edit(
    p_lessonid INT, p_lessontypeid INT, p_lessonorder INT, p_isactive BOOLEAN,
    p_languageid INT, p_lessonname VARCHAR, p_description VARCHAR
) RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    UPDATE lesson SET lessontypeid = p_lessontypeid, lessonorder = p_lessonorder, isactive = p_isactive
    WHERE lessonid = p_lessonid;

    UPDATE lesson_lang SET lessonname = p_lessonname, description = p_description
    WHERE lessonid = p_lessonid AND languageid = p_languageid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_lesson_delete(p_lessonid INT)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM lesson_lang WHERE lessonid = p_lessonid;
    DELETE FROM lesson WHERE lessonid = p_lessonid;
END;
$$;

-- =============================================
-- MEANING FUNCTIONS
-- =============================================

CREATE OR REPLACE FUNCTION sp_meaningquestion_get(p_lessonid INT, p_languageid INT)
RETURNS TABLE (
    questionid INT, questiontext VARCHAR, optionid INT, optiontext VARCHAR
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT mq.questionid, mql.questiontext, mo.optionid, mol.optiontext
    FROM meaningquestion mq
    JOIN meaningquestion_lang mql ON mq.questionid = mql.questionid AND mql.languageid = p_languageid
    JOIN meaningoption mo ON mq.questionid = mo.questionid
    JOIN meaningoption_lang mol ON mo.optionid = mol.optionid AND mol.languageid = p_languageid
    WHERE mq.lessonid = p_lessonid
    ORDER BY mq.questionid, mo.optionid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_meaningquestion_getwithansw(p_lessonid INT, p_languageid INT)
RETURNS TABLE (
    questionid INT, questiontext VARCHAR, optionid INT, optiontext VARCHAR, iscorrect BOOLEAN
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT mq.questionid, mql.questiontext, mo.optionid, mol.optiontext, mo.iscorrect
    FROM meaningquestion mq
    JOIN meaningquestion_lang mql ON mq.questionid = mql.questionid AND mql.languageid = p_languageid
    JOIN meaningoption mo ON mq.questionid = mo.questionid
    JOIN meaningoption_lang mol ON mo.optionid = mol.optionid AND mol.languageid = p_languageid
    WHERE mq.lessonid = p_lessonid
    ORDER BY mq.questionid, mo.optionid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_meaningquestion_add(p_lessonid INT, p_languageid INT, p_questiontext VARCHAR)
RETURNS INT LANGUAGE plpgsql AS $$
DECLARE v_qid INT;
BEGIN
    INSERT INTO meaningquestion (lessonid) VALUES (p_lessonid) RETURNING questionid INTO v_qid;
    INSERT INTO meaningquestion_lang (questionid, languageid, questiontext) VALUES (v_qid, p_languageid, p_questiontext);
    RETURN v_qid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_meaningquestion_edit(p_questionid INT, p_languageid INT, p_questiontext VARCHAR)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    UPDATE meaningquestion_lang SET questiontext = p_questiontext
    WHERE questionid = p_questionid AND languageid = p_languageid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_meaningquestion_delete(p_questionid INT)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM meaningoption_lang WHERE optionid IN (SELECT optionid FROM meaningoption WHERE questionid = p_questionid);
    DELETE FROM meaningoption WHERE questionid = p_questionid;
    DELETE FROM meaningquestion_lang WHERE questionid = p_questionid;
    DELETE FROM meaningquestion WHERE questionid = p_questionid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_meaningoption_add(p_questionid INT, p_languageid INT, p_optiontext VARCHAR, p_iscorrect BOOLEAN)
RETURNS INT LANGUAGE plpgsql AS $$
DECLARE v_oid INT;
BEGIN
    INSERT INTO meaningoption (questionid, iscorrect) VALUES (p_questionid, p_iscorrect) RETURNING optionid INTO v_oid;
    INSERT INTO meaningoption_lang (optionid, languageid, optiontext) VALUES (v_oid, p_languageid, p_optiontext);
    RETURN v_oid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_meaningoption_edit(p_optionid INT, p_languageid INT, p_optiontext VARCHAR, p_iscorrect BOOLEAN)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    UPDATE meaningoption SET iscorrect = p_iscorrect WHERE optionid = p_optionid;
    UPDATE meaningoption_lang SET optiontext = p_optiontext WHERE optionid = p_optionid AND languageid = p_languageid;
END;
$$;

CREATE OR REPLACE FUNCTION sp_meaningoption_delete(p_optionid INT)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM meaningoption_lang WHERE optionid = p_optionid;
    DELETE FROM meaningoption WHERE optionid = p_optionid;
END;
$$;

-- =============================================
-- ARRANGE SENTENCE FUNCTIONS
-- =============================================

CREATE OR REPLACE FUNCTION sp_arrangesentence_get(p_lessonid INT, p_languageid INT)
RETURNS TABLE (
    arrangesentenceid INT, correctsentence VARCHAR,
    wordid INT, wordtext VARCHAR, correctorder INT
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT asn.arrangesentenceid, asl.correctsentence, asw.wordid, aswl.wordtext, asw.correctorder
    FROM arrangesentence asn
    JOIN arrangesentence_lang asl ON asn.arrangesentenceid = asl.arrangesentenceid AND asl.languageid = p_languageid
    JOIN arrangesentenceword asw ON asn.arrangesentenceid = asw.arrangesentenceid
    JOIN arrangesentenceword_lang aswl ON asw.wordid = aswl.wordid AND aswl.languageid = p_languageid
    WHERE asn.lessonid = p_lessonid
    ORDER BY asw.correctorder;
END;
$$;

-- =============================================
-- READING FUNCTIONS
-- =============================================

CREATE OR REPLACE FUNCTION sp_readingsentence_get(p_lessonid INT, p_languageid INT)
RETURNS TABLE (
    readingsentenceid INT, sentencetext VARCHAR, referenceaudiourl VARCHAR
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT rs.readingsentenceid, rsl.sentencetext, rsl.referenceaudiourl
    FROM readingsentence rs
    JOIN readingsentence_lang rsl ON rs.readingsentenceid = rsl.readingsentenceid
    WHERE rs.lessonid = p_lessonid AND rsl.languageid = p_languageid;
END;
$$;

-- =============================================
-- PROGRESS FUNCTIONS
-- =============================================

CREATE OR REPLACE FUNCTION sp_useranswer_save(
    p_userid INT, p_lessonid INT, p_languageid INT,
    p_activitytype VARCHAR, p_referenceid INT, p_iscorrect BOOLEAN
) RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO useranswer (userid, lessonid, languageid, activitytype, referenceid, iscorrect)
    VALUES (p_userid, p_lessonid, p_languageid, p_activitytype, p_referenceid, p_iscorrect);
END;
$$;

CREATE OR REPLACE FUNCTION sp_userprogress_get(p_userid INT, p_languageid INT)
RETURNS TABLE (lessonid INT, totalattempt BIGINT, correctcount BIGINT)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT ua.lessonid,
           COUNT(*),
           SUM(CASE WHEN ua.iscorrect = true THEN 1 ELSE 0 END)
    FROM useranswer ua
    WHERE ua.userid = p_userid AND ua.languageid = p_languageid
    GROUP BY ua.lessonid;
END;
$$;

-- =============================================
-- API USAGE FUNCTION
-- =============================================

CREATE OR REPLACE FUNCTION sp_insert_apiusage(p_userguid UUID, p_endpoint VARCHAR)
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO apiusage (userguid, endpoint, requestcount, usagedate)
    VALUES (p_userguid, p_endpoint, 1, CURRENT_DATE)
    ON CONFLICT (userguid, endpoint, usagedate)
    DO UPDATE SET requestcount = apiusage.requestcount + 1;
END;
$$;
