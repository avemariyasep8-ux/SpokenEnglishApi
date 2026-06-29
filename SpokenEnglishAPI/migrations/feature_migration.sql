-- ================================================================
-- Feature Migration: 6 New Features for Spoken English App
-- Run this on production PostgreSQL (Railway)
-- Safe to re-run: uses IF NOT EXISTS / ON CONFLICT DO NOTHING
-- ================================================================

-- ── Feature 2: Lesson Levels ──────────────────────────────────────
ALTER TABLE lesson ADD COLUMN IF NOT EXISTS level VARCHAR(50) DEFAULT 'Beginner';
ALTER TABLE lesson ADD COLUMN IF NOT EXISTS is_premium BOOLEAN DEFAULT false;

-- ── Feature 3: Progress & Streaks ────────────────────────────────
CREATE TABLE IF NOT EXISTS user_lesson_progress (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    lesson_id INT REFERENCES lesson(lessonid) ON DELETE CASCADE,
    is_completed BOOLEAN DEFAULT false,
    completed_date TIMESTAMP,
    time_spent_seconds INT DEFAULT 0,
    correct_answers INT DEFAULT 0,
    wrong_answers INT DEFAULT 0,
    total_attempts INT DEFAULT 0,
    last_activity TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, lesson_id)
);

CREATE TABLE IF NOT EXISTS user_streaks (
    id SERIAL PRIMARY KEY,
    user_id INT UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    current_streak INT DEFAULT 0,
    longest_streak INT DEFAULT 0,
    total_xp INT DEFAULT 0,
    last_activity_date DATE
);

CREATE TABLE IF NOT EXISTS daily_activity (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    activity_date DATE NOT NULL DEFAULT CURRENT_DATE,
    lessons_completed INT DEFAULT 0,
    xp_earned INT DEFAULT 0,
    time_spent_seconds INT DEFAULT 0,
    UNIQUE(user_id, activity_date)
);

-- ── Feature 4: School Management ─────────────────────────────────
CREATE TABLE IF NOT EXISTS schools (
    school_id SERIAL PRIMARY KEY,
    school_name VARCHAR(200) NOT NULL,
    school_code VARCHAR(50) UNIQUE,
    address VARCHAR(500),
    contact_email VARCHAR(200),
    contact_phone VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_date TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS school_users (
    id SERIAL PRIMARY KEY,
    school_id INT REFERENCES schools(school_id) ON DELETE CASCADE,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    school_role VARCHAR(50) NOT NULL DEFAULT 'Student',
    class_name VARCHAR(100),
    is_approved BOOLEAN DEFAULT false,
    approved_by INT REFERENCES users(id),
    created_date TIMESTAMP DEFAULT NOW(),
    UNIQUE(school_id, user_id)
);

CREATE TABLE IF NOT EXISTS teacher_student_mapping (
    id SERIAL PRIMARY KEY,
    teacher_user_id INT REFERENCES users(id) ON DELETE CASCADE,
    student_user_id INT REFERENCES users(id) ON DELETE CASCADE,
    school_id INT REFERENCES schools(school_id) ON DELETE CASCADE,
    created_date TIMESTAMP DEFAULT NOW(),
    UNIQUE(teacher_user_id, student_user_id)
);

-- Add school fields to users if not present
ALTER TABLE users ADD COLUMN IF NOT EXISTS school_id INT REFERENCES schools(school_id);
ALTER TABLE users ADD COLUMN IF NOT EXISTS school_role VARCHAR(50);
ALTER TABLE users ADD COLUMN IF NOT EXISTS full_name VARCHAR(200);

-- ── Feature 5: Subscription & Access ─────────────────────────────
CREATE TABLE IF NOT EXISTS subscription_plans (
    plan_id SERIAL PRIMARY KEY,
    plan_name VARCHAR(100) NOT NULL,
    duration_months INT NOT NULL DEFAULT 1,
    price_inr DECIMAL(10,2) NOT NULL DEFAULT 0,
    max_lessons INT DEFAULT NULL,
    features TEXT,
    is_active BOOLEAN DEFAULT true,
    created_date TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_subscriptions (
    subscription_id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    plan_id INT REFERENCES subscription_plans(plan_id),
    start_date TIMESTAMP DEFAULT NOW(),
    end_date TIMESTAMP,
    status VARCHAR(20) DEFAULT 'active',
    payment_ref VARCHAR(200),
    created_date TIMESTAMP DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS user_lesson_access (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    lesson_id INT REFERENCES lesson(lessonid) ON DELETE CASCADE,
    has_access BOOLEAN DEFAULT true,
    granted_date TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, lesson_id)
);

-- ── Indexes for performance ───────────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_ulp_user ON user_lesson_progress(user_id);
CREATE INDEX IF NOT EXISTS idx_ulp_lesson ON user_lesson_progress(lesson_id);
CREATE INDEX IF NOT EXISTS idx_daily_user ON daily_activity(user_id, activity_date);
CREATE INDEX IF NOT EXISTS idx_school_users_school ON school_users(school_id);
CREATE INDEX IF NOT EXISTS idx_school_users_user ON school_users(user_id);
CREATE INDEX IF NOT EXISTS idx_teacher_student ON teacher_student_mapping(teacher_user_id);
CREATE INDEX IF NOT EXISTS idx_lesson_level ON lesson(level);

-- ── Update sp_lesson_get to include level and is_premium ──────────
DROP FUNCTION IF EXISTS sp_lesson_get(INT);
CREATE OR REPLACE FUNCTION sp_lesson_get(p_languageid INT)
RETURNS TABLE (
    lessonid INT, lessontype VARCHAR, lessonname VARCHAR,
    description VARCHAR, lessonorder INT, level VARCHAR, is_premium BOOLEAN
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT l.lessonid, lt.typename, ll.lessonname, ll.description,
           l.lessonorder, COALESCE(l.level, 'Beginner'), COALESCE(l.is_premium, false)
    FROM lesson l
    JOIN lessontype lt ON l.lessontypeid = lt.lessontypeid
    JOIN lesson_lang ll ON l.lessonid = ll.lessonid
    WHERE ll.languageid = p_languageid AND l.isactive = true
    ORDER BY l.lessonorder;
END;
$$;

-- ── New stored procedures for schools ─────────────────────────────
CREATE OR REPLACE FUNCTION sp_school_get_all()
RETURNS TABLE (
    school_id INT, school_name VARCHAR, school_code VARCHAR,
    contact_email VARCHAR, contact_phone VARCHAR, is_active BOOLEAN,
    student_count BIGINT, teacher_count BIGINT
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT s.school_id, s.school_name, s.school_code, s.contact_email, s.contact_phone, s.is_active,
           COUNT(CASE WHEN su.school_role = 'Student' THEN 1 END),
           COUNT(CASE WHEN su.school_role = 'Teacher' THEN 1 END)
    FROM schools s
    LEFT JOIN school_users su ON s.school_id = su.school_id AND su.is_approved = true
    GROUP BY s.school_id, s.school_name, s.school_code, s.contact_email, s.contact_phone, s.is_active
    ORDER BY s.school_name;
END;
$$;

CREATE OR REPLACE FUNCTION sp_school_students_progress(p_school_id INT, p_teacher_user_id INT DEFAULT NULL)
RETURNS TABLE (
    user_id INT, email VARCHAR, full_name VARCHAR, school_role VARCHAR,
    lessons_completed BIGINT, total_xp INT, current_streak INT, last_activity DATE
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.email, COALESCE(u.full_name, u.email), su.school_role,
           COUNT(ulp.id) FILTER (WHERE ulp.is_completed = true),
           COALESCE(us.total_xp, 0),
           COALESCE(us.current_streak, 0),
           us.last_activity_date
    FROM school_users su
    JOIN users u ON u.id = su.user_id
    LEFT JOIN user_lesson_progress ulp ON ulp.user_id = u.id
    LEFT JOIN user_streaks us ON us.user_id = u.id
    WHERE su.school_id = p_school_id
      AND su.school_role = 'Student'
      AND su.is_approved = true
      AND (p_teacher_user_id IS NULL OR EXISTS (
          SELECT 1 FROM teacher_student_mapping tsm
          WHERE tsm.teacher_user_id = p_teacher_user_id AND tsm.student_user_id = u.id
      ))
    GROUP BY u.id, u.email, u.full_name, su.school_role, us.total_xp, us.current_streak, us.last_activity_date
    ORDER BY lessons_completed DESC;
END;
$$;

-- ── Progress: weekly activity ──────────────────────────────────────
CREATE OR REPLACE FUNCTION sp_user_weekly_activity(p_user_id INT)
RETURNS TABLE (
    activity_date DATE, lessons_completed INT, xp_earned INT, time_spent_seconds INT
) LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    SELECT da.activity_date, COALESCE(da.lessons_completed, 0),
           COALESCE(da.xp_earned, 0), COALESCE(da.time_spent_seconds, 0)
    FROM generate_series(CURRENT_DATE - INTERVAL '6 days', CURRENT_DATE, '1 day') AS gs(d)
    LEFT JOIN daily_activity da ON da.activity_date = gs.d::DATE AND da.user_id = p_user_id
    ORDER BY gs.d;
END;
$$;

-- ── Sample Data ───────────────────────────────────────────────────

-- Level assignments for existing lessons (lessonorder 1-5 = Beginner, 6-10 = Elementary, 11-12 = Intermediate)
UPDATE lesson SET level = 'Beginner'     WHERE lessonorder BETWEEN 1  AND 5;
UPDATE lesson SET level = 'Elementary'   WHERE lessonorder BETWEEN 6  AND 10;
UPDATE lesson SET level = 'Intermediate' WHERE lessonorder BETWEEN 11 AND 15;
UPDATE lesson SET level = 'College'      WHERE lessonorder BETWEEN 16 AND 20;
UPDATE lesson SET level = 'Professional' WHERE lessonorder > 20;

-- Subscription plans
INSERT INTO subscription_plans (plan_name, duration_months, price_inr, features, is_active)
VALUES
  ('Free',         0,  0.00,   'Access to Lessons 1-5, Basic Progress Tracking', true),
  ('Basic',        1,  99.00,  'Access to Lessons 1-10, Progress Dashboard, Streak Tracking', true),
  ('Pro',          3,  249.00, 'All Lessons, School Progress Reports, Priority Support', true),
  ('Premium',      12, 799.00, 'All Lessons, School Management, Analytics, Certificate', true)
ON CONFLICT DO NOTHING;

-- Sample school
INSERT INTO schools (school_name, school_code, address, contact_email, contact_phone)
VALUES ('Demo School', 'DEMO001', '123 Education Street, Chennai', 'admin@demoschool.edu', '9876543210')
ON CONFLICT DO NOTHING;
