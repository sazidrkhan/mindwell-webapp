-- ==========================================
-- MindWell Database Seed Data Script
-- ==========================================
-- This script populates the MindWell database with initial data
-- NOTE: Password hashes are generated using PBKDF2. 
-- For development/testing purposes only.

-- USE MindWellDB;
-- GO

-- ==========================================
-- 1. Insert User Roles
-- ==========================================
INSERT INTO UserRoles (RoleName, Description) VALUES
('Admin', 'Administrator with full system access'),
('User', 'Authenticated user - can access learning content'),
('Guest', 'Unauthenticated public user');

PRINT 'User Roles inserted';

-- ==========================================
-- 2. Insert Learning Modules
-- ==========================================
INSERT INTO LearningModules (ModuleName, Description, OrderIndex, CreatedDate) VALUES
('Introduction to Mental Health', 'Understand the fundamentals of mental health, common conditions, and available resources.', 1, GETDATE()),
('Stress Management', 'Learn evidence-based techniques to manage stress and build resilience.', 2, GETDATE()),
('Mindfulness', 'Explore mindfulness practices to enhance awareness and reduce anxiety.', 3, GETDATE()),
('Self-Care', 'Develop personalized self-care strategies for overall wellbeing.', 4, GETDATE());

PRINT 'Learning Modules inserted';

-- ==========================================
-- 3. Insert Sample Articles
-- ==========================================
INSERT INTO Articles (ModuleID, Title, Content, IsPublished, CreatedDate) VALUES
(1, 'What is Mental Health?', '<p>Mental health is a state of well-being in which every individual realizes his or her own potential, can cope with the normal stresses of life, can work productively and fruitfully, and is able to make a contribution to her or his community.</p><p>Mental health is fundamental to our overall health and well-being. It is the foundation for thinking, communicating, learning, earning, relating to others, and realizing our potential.</p>', 1, GETDATE()),
(1, 'Common Mental Health Conditions', '<p>Understanding common mental health conditions helps with identification and support. Depression, anxiety, PTSD, and other conditions affect millions worldwide.</p><p>Seeking help early can lead to better outcomes and quality of life.</p>', 1, GETDATE()),
(2, 'Understanding Stress', '<p>Stress is a normal reaction to everyday pressures. It can be positive, providing us with motivation to accomplish tasks and meet goals.</p><p>However, when stress becomes overwhelming and persistent, it can negatively impact our health and well-being.</p>', 1, GETDATE()),
(2, 'Stress Management Techniques', '<p>Evidence-based techniques for managing stress include breathing exercises, progressive muscle relaxation, time management, and seeking social support.</p>', 1, GETDATE()),
(3, 'Introduction to Mindfulness', '<p>Mindfulness is the practice of paying attention to the present moment with openness and without judgment. It helps reduce anxiety and improve emotional regulation.</p>', 1, GETDATE()),
(3, 'Meditation Basics', '<p>Meditation is a core mindfulness practice that trains your attention and awareness. Even 5 minutes daily can provide significant benefits.</p>', 1, GETDATE()),
(4, 'What is Self-Care?', '<p>Self-care refers to activities that promote physical, mental, and emotional health. It is not selfish; it is essential for well-being.</p>', 1, GETDATE()),
(4, 'Building a Self-Care Routine', '<p>Creating sustainable self-care habits requires planning and commitment. Start small and build gradually for lasting change.</p>', 1, GETDATE());

PRINT 'Articles inserted';

-- ==========================================
-- 4. Insert Quizzes
-- ==========================================
INSERT INTO Quizzes (ModuleID, QuizTitle, Description, PassingScore, CreatedDate) VALUES
(1, 'Introduction to Mental Health Quiz', 'Test your knowledge on mental health fundamentals.', 70, GETDATE()),
(2, 'Stress Management Quiz', 'Assess your understanding of stress management techniques.', 70, GETDATE()),
(3, 'Mindfulness Quiz', 'Evaluate your knowledge of mindfulness practices.', 70, GETDATE()),
(4, 'Self-Care Quiz', 'Test your understanding of self-care principles.', 70, GETDATE());

PRINT 'Quizzes inserted';

-- ==========================================
-- 5. Insert Quiz Questions (Module 1: Mental Health)
-- ==========================================
INSERT INTO QuizQuestions (QuizID, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, OrderIndex) VALUES
(1, 'What is mental health?', 'A state of well-being', 'Only the absence of mental illness', 'A permanent condition', 'Determined solely by genetics', 'A', 1),
(1, 'Which is NOT a common mental health condition?', 'Depression', 'Anxiety Disorder', 'Cardiovascular health', 'ADHD', 'C', 2),
(1, 'When should you seek professional help for mental health concerns?', 'When symptoms persist for 2+ weeks', 'When coping mechanisms fail', 'Both A and B', 'Never - handle it alone', 'C', 3),
(1, 'What is the role of social support in mental health?', 'Irrelevant to mental health', 'Critical for recovery and wellbeing', 'Only helpful for severe conditions', 'Something to avoid', 'B', 4),
(1, 'Which of these is a positive coping mechanism?', 'Substance use', 'Exercise and social connection', 'Avoidance', 'Aggression', 'B', 5);

-- ==========================================
-- 6. Insert Quiz Questions (Module 2: Stress Management)
-- ==========================================
INSERT INTO QuizQuestions (QuizID, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, OrderIndex) VALUES
(2, 'What is the first step in stress management?', 'Meditation', 'Identifying your stressors', 'Avoiding all stress', 'Ignoring the problem', 'B', 1),
(2, 'Which technique activates the parasympathetic nervous system?', 'Caffeine consumption', 'Deep breathing exercises', 'Intense exercise', 'Multi-tasking', 'B', 2),
(2, 'How long should daily stress management practice be?', '5 minutes minimum', '30 minutes minimum', '1 hour minimum', 'Any amount is beneficial', 'D', 3),
(2, 'What role does sleep play in stress management?', 'Unimportant', 'Critical for stress recovery', 'Only matters if stressed', 'Increases stress', 'B', 4),
(2, 'Which is an effective stress management strategy?', 'Suppressing emotions', 'Setting healthy boundaries', 'Overworking', 'Isolation', 'B', 5);

-- ==========================================
-- 7. Insert Quiz Questions (Module 3: Mindfulness)
-- ==========================================
INSERT INTO QuizQuestions (QuizID, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, OrderIndex) VALUES
(3, 'What is mindfulness?', 'Thinking about the future', 'Present-moment awareness without judgment', 'Meditation only', 'Relaxation technique', 'B', 1),
(3, 'How often should you meditate for benefits?', 'Only on weekends', 'Daily practice recommended', 'Once a month', 'Never necessary', 'B', 2),
(3, 'What is the goal of mindfulness meditation?', 'Empty your mind', 'Observe thoughts without judgment', 'Fall asleep', 'Achieve perfection', 'B', 3),
(3, 'Which brain area is affected by regular mindfulness?', 'Amygdala (fear center)', 'Prefrontal cortex (planning)', 'Both A and B', 'Neither', 'C', 4),
(3, 'How long does mindfulness take to show benefits?', '1 week', '2-3 weeks', '8 weeks with consistency', 'Never shows benefits', 'C', 5);

-- ==========================================
-- 8. Insert Quiz Questions (Module 4: Self-Care)
-- ==========================================
INSERT INTO QuizQuestions (QuizID, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, OrderIndex) VALUES
(4, 'Is self-care selfish?', 'Yes, always', 'No, it is essential', 'Sometimes', 'Only for some people', 'B', 1),
(4, 'Which is an example of physical self-care?', 'Therapy', 'Sleep and exercise', 'Meditation', 'Journaling', 'B', 2),
(4, 'How many hours of sleep is recommended nightly?', '4-5 hours', '6-7 hours', '7-9 hours', '10+ hours', 'C', 3),
(4, 'Which pillar of self-care includes relationships?', 'Physical', 'Mental', 'Social', 'Spiritual', 'C', 4),
(4, 'What is the best approach to self-care?', 'All or nothing', 'Expensive activities only', 'Small, consistent practices', 'No specific approach needed', 'C', 5);

PRINT 'Quiz Questions inserted';

-- ==========================================
-- 9. Insert Demo User Accounts
-- ==========================================
-- These local coursework demo accounts match the automatic initializer.
-- Runtime registrations and automatic initialization use PBKDF2 through PasswordHelper.HashPassword().
-- This standalone SQL seed keeps legacy SHA-256 hashes because PasswordHelper supports them for seed compatibility.

INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, RoleID, CreatedDate, IsActive, IsBanned, TotpSecret, IsTotpEnabled, PasswordResetRequired) VALUES
('admin', 'admin@mindwell.local', '3EB3FE66B31E3B4D10FA70B5CAD49C7112294AF6AE4E476A1C405155D45AA121', 'Admin', 'User', 1, GETDATE(), 1, 0, NULL, 0, 0),
('student1', 'student1@mindwell.local', 'CB2433073EDB25CA77F0445DCDBD7E7C568196EEF1CD54DA7228DDD227240E2E', 'Student', 'One', 2, GETDATE(), 1, 0, NULL, 0, 0),
('sazidrkhan', 'sazidrkhan@mindwell.local', '00EF7C2FF5FDEA4794164BA3B3CBAB33C005127DC6717430617E486A88BA9129', 'Sazid R', 'Khan', 1, GETDATE(), 1, 0, NULL, 0, 0),
('yal', 'yal@mindwell.local', '3F3E7A5BB347988B4BC723BD986A12E1624FB0AC4260476F76972D355833F81B', 'Mariyam Iyala', 'Ali', 1, GETDATE(), 1, 0, NULL, 0, 0),
('eem', 'eem@mindwell.local', '3635531D995E1CF301029E2346BF017D7F312548F13D112637D2D0043B296D87', 'Mariyam Eemau', 'Riza', 1, GETDATE(), 1, 0, NULL, 0, 0),
('esha', 'esha@mindwell.local', '48C5A7AD6EE6D5FB126CDC1B8863D94DD5132C9A8446351A90B2849A5F957C49', 'Esha Tariq', 'Want', 1, GETDATE(), 1, 0, NULL, 0, 0),
('lecturer', 'lecturer@mindwell.local', 'DBF4427B0863FE858CBF550DB4FA0AB7DD0803A1C75254E3F85953F337807B94', 'Lecturer', '', 1, GETDATE(), 1, 0, NULL, 0, 0),
('student2', 'student2@mindwell.local', 'CB2433073EDB25CA77F0445DCDBD7E7C568196EEF1CD54DA7228DDD227240E2E', 'Demo Student', 'Two', 2, GETDATE(), 1, 0, NULL, 0, 0),
('student3', 'student3@mindwell.local', 'CB2433073EDB25CA77F0445DCDBD7E7C568196EEF1CD54DA7228DDD227240E2E', 'Demo Student', 'Three', 2, GETDATE(), 1, 0, NULL, 0, 0);

PRINT '9 Demo User Accounts inserted (admin, student1, sazidrkhan, yal, eem, esha, lecturer, student2, student3)';

-- ==========================================
-- 10. Insert Default Settings
-- ==========================================
INSERT INTO Settings (SettingKey, SettingValue, DataType, UpdatedDate) VALUES
('SiteName', 'MindWell: Mental Health Learning Platform', 'string', GETDATE()),
('SiteEmail', 'admin@mindwell.local', 'string', GETDATE()),
('LogoURL', '/Images/logo.png', 'string', GETDATE()),
('MaintenanceMode', 'false', 'bool', GETDATE()),
('SessionTimeout', '30', 'int', GETDATE());

PRINT 'Settings inserted';

-- ==========================================
-- SUMMARY
-- ==========================================
PRINT '============================================';
PRINT 'MindWell Database Seed Data Completed!';
PRINT '============================================';
PRINT 'Created:';
PRINT '  - 3 User Roles';
PRINT '  - 4 Learning Modules';
PRINT '  - 8 Articles';
PRINT '  - 4 Quizzes with 5 questions each (20 total)';
PRINT '  - 9 Demo User Accounts (admin, student1, sazidrkhan, yal, eem, esha, lecturer, student2, student3)';
PRINT '  - Default Settings';
PRINT '';
PRINT 'Demo users are ready for testing.';
PRINT '============================================';
GO
