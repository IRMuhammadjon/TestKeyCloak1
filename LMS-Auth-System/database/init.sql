-- LMS Database Initialization Script

-- Create schema if not exists
CREATE SCHEMA IF NOT EXISTS lms;

-- Set search path
SET search_path TO lms, public;

-- Users table (External DB - asosiy LMS ma'lumotlari)
CREATE TABLE IF NOT EXISTS lms.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    keycloak_id VARCHAR(255) UNIQUE,
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID
);

-- Roles table
CREATE TABLE IF NOT EXISTS lms.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    keycloak_id VARCHAR(255) UNIQUE,
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID
);

-- Permissions table
CREATE TABLE IF NOT EXISTS lms.permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) UNIQUE NOT NULL,
    resource VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    CONSTRAINT unique_resource_action UNIQUE (resource, action)
);

-- User-Role mapping (Many-to-Many)
CREATE TABLE IF NOT EXISTS lms.user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID,
    CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES lms.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_role FOREIGN KEY (role_id) REFERENCES lms.roles(id) ON DELETE CASCADE,
    CONSTRAINT unique_user_role UNIQUE (user_id, role_id)
);

-- User-Permission mapping (Many-to-Many)
CREATE TABLE IF NOT EXISTS lms.user_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    permission_id UUID NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID,
    CONSTRAINT fk_user_perm FOREIGN KEY (user_id) REFERENCES lms.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_permission FOREIGN KEY (permission_id) REFERENCES lms.permissions(id) ON DELETE CASCADE,
    CONSTRAINT unique_user_permission UNIQUE (user_id, permission_id)
);

-- Role-Permission mapping (Many-to-Many)
CREATE TABLE IF NOT EXISTS lms.role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL,
    permission_id UUID NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID,
    CONSTRAINT fk_role_perm FOREIGN KEY (role_id) REFERENCES lms.roles(id) ON DELETE CASCADE,
    CONSTRAINT fk_permission_role FOREIGN KEY (permission_id) REFERENCES lms.permissions(id) ON DELETE CASCADE,
    CONSTRAINT unique_role_permission UNIQUE (role_id, permission_id)
);

-- Sync History table (Keycloak sinxronizatsiya tarixini saqlash)
CREATE TABLE IF NOT EXISTS lms.sync_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID NOT NULL,
    operation VARCHAR(20) NOT NULL,
    keycloak_id VARCHAR(255),
    status VARCHAR(20) NOT NULL,
    error_message TEXT,
    synced_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for better performance
CREATE INDEX IF NOT EXISTS idx_users_username ON lms.users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON lms.users(email);
CREATE INDEX IF NOT EXISTS idx_users_keycloak_id ON lms.users(keycloak_id);
CREATE INDEX IF NOT EXISTS idx_roles_name ON lms.roles(name);
CREATE INDEX IF NOT EXISTS idx_roles_keycloak_id ON lms.roles(keycloak_id);
CREATE INDEX IF NOT EXISTS idx_permissions_resource_action ON lms.permissions(resource, action);
CREATE INDEX IF NOT EXISTS idx_user_roles_user ON lms.user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role ON lms.user_roles(role_id);
CREATE INDEX IF NOT EXISTS idx_sync_history_entity ON lms.sync_history(entity_type, entity_id);

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION lms.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers for updated_at
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON lms.users
    FOR EACH ROW EXECUTE FUNCTION lms.update_updated_at_column();

CREATE TRIGGER update_roles_updated_at BEFORE UPDATE ON lms.roles
    FOR EACH ROW EXECUTE FUNCTION lms.update_updated_at_column();

CREATE TRIGGER update_permissions_updated_at BEFORE UPDATE ON lms.permissions
    FOR EACH ROW EXECUTE FUNCTION lms.update_updated_at_column();

-- Insert default roles
INSERT INTO lms.roles (name, description) VALUES
    ('admin', 'Administrator role with full access'),
    ('teacher', 'Teacher role for LMS'),
    ('student', 'Student role for LMS'),
    ('user', 'Standard user role')
ON CONFLICT (name) DO NOTHING;

-- Insert default permissions
INSERT INTO lms.permissions (name, resource, action, description) VALUES
    -- User permissions
    ('users.create', 'users', 'create', 'Create new users'),
    ('users.read', 'users', 'read', 'View user information'),
    ('users.update', 'users', 'update', 'Update user information'),
    ('users.delete', 'users', 'delete', 'Delete users'),
    ('users.list', 'users', 'list', 'List all users'),

    -- Role permissions
    ('roles.create', 'roles', 'create', 'Create new roles'),
    ('roles.read', 'roles', 'read', 'View role information'),
    ('roles.update', 'roles', 'update', 'Update role information'),
    ('roles.delete', 'roles', 'delete', 'Delete roles'),
    ('roles.list', 'roles', 'list', 'List all roles'),
    ('roles.assign', 'roles', 'assign', 'Assign roles to users'),

    -- Permission permissions
    ('permissions.create', 'permissions', 'create', 'Create new permissions'),
    ('permissions.read', 'permissions', 'read', 'View permission information'),
    ('permissions.update', 'permissions', 'update', 'Update permission information'),
    ('permissions.delete', 'permissions', 'delete', 'Delete permissions'),
    ('permissions.list', 'permissions', 'list', 'List all permissions'),
    ('permissions.assign', 'permissions', 'assign', 'Assign permissions to users or roles'),

    -- Course permissions (example for LMS)
    ('courses.create', 'courses', 'create', 'Create new courses'),
    ('courses.read', 'courses', 'read', 'View course information'),
    ('courses.update', 'courses', 'update', 'Update course information'),
    ('courses.delete', 'courses', 'delete', 'Delete courses'),
    ('courses.list', 'courses', 'list', 'List all courses'),
    ('courses.enroll', 'courses', 'enroll', 'Enroll in courses')
ON CONFLICT (resource, action) DO NOTHING;

-- Assign permissions to admin role
INSERT INTO lms.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM lms.roles r
CROSS JOIN lms.permissions p
WHERE r.name = 'admin'
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Assign basic permissions to teacher role
INSERT INTO lms.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM lms.roles r
CROSS JOIN lms.permissions p
WHERE r.name = 'teacher'
    AND p.name IN ('courses.create', 'courses.read', 'courses.update', 'courses.list', 'users.read', 'users.list')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Assign basic permissions to student role
INSERT INTO lms.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM lms.roles r
CROSS JOIN lms.permissions p
WHERE r.name = 'student'
    AND p.name IN ('courses.read', 'courses.list', 'courses.enroll')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Assign basic permissions to user role
INSERT INTO lms.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM lms.roles r
CROSS JOIN lms.permissions p
WHERE r.name = 'user'
    AND p.name IN ('users.read', 'courses.read', 'courses.list')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Grant privileges
GRANT ALL PRIVILEGES ON SCHEMA lms TO lms_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA lms TO lms_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA lms TO lms_user;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA lms TO lms_user;
