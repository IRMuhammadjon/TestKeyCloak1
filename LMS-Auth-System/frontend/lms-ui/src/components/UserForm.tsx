import React, { useState, useEffect } from 'react';
import { userApi, roleApi } from '../services/api';
import { User, CreateUserRequest, UpdateUserRequest, Role } from '../types';
import './UserForm.css';

interface UserFormProps {
  user?: User;
  onSuccess: () => void;
  onCancel: () => void;
}

const UserForm: React.FC<UserFormProps> = ({ user, onSuccess, onCancel }) => {
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    firstName: '',
    lastName: '',
    phone: '',
    password: '',
    isActive: true,
  });
  const [roles, setRoles] = useState<Role[]>([]);
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadRoles();
    if (user) {
      setFormData({
        username: user.username,
        email: user.email,
        firstName: user.firstName,
        lastName: user.lastName,
        phone: user.phone || '',
        password: '',
        isActive: user.isActive,
      });
      setSelectedRoles(user.userRoles?.map(ur => ur.role.id) || []);
    }
  }, [user]);

  const loadRoles = async () => {
    try {
      const data = await roleApi.getAll();
      setRoles(data);
    } catch (err) {
      console.error('Error loading roles:', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      if (user) {
        // Update existing user
        const updateData: UpdateUserRequest = {
          email: formData.email,
          firstName: formData.firstName,
          lastName: formData.lastName,
          phone: formData.phone || undefined,
          isActive: formData.isActive,
        };
        await userApi.update(user.id, updateData);

        // Update roles
        const currentRoles = user.userRoles?.map(ur => ur.role.id) || [];
        const rolesToAdd = selectedRoles.filter(r => !currentRoles.includes(r));
        const rolesToRemove = currentRoles.filter(r => !selectedRoles.includes(r));

        for (const roleId of rolesToAdd) {
          await userApi.assignRole(user.id, roleId);
        }
        for (const roleId of rolesToRemove) {
          await userApi.removeRole(user.id, roleId);
        }
      } else {
        // Create new user
        const createData: CreateUserRequest = {
          username: formData.username,
          email: formData.email,
          firstName: formData.firstName,
          lastName: formData.lastName,
          phone: formData.phone || undefined,
          password: formData.password,
        };
        const newUser = await userApi.create(createData);

        // Assign roles
        for (const roleId of selectedRoles) {
          await userApi.assignRole(newUser.id, roleId);
        }
      }

      onSuccess();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save user');
      console.error('Error saving user:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleRoleToggle = (roleId: string) => {
    setSelectedRoles(prev =>
      prev.includes(roleId)
        ? prev.filter(id => id !== roleId)
        : [...prev, roleId]
    );
  };

  return (
    <div className="user-form">
      <h2>{user ? 'Edit User' : 'Create New User'}</h2>
      {error && <div className="error">{error}</div>}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Username *</label>
          <input
            type="text"
            name="username"
            value={formData.username}
            onChange={handleChange}
            disabled={!!user}
            required
          />
        </div>

        <div className="form-group">
          <label>Email *</label>
          <input
            type="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            required
          />
        </div>

        <div className="form-row">
          <div className="form-group">
            <label>First Name *</label>
            <input
              type="text"
              name="firstName"
              value={formData.firstName}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group">
            <label>Last Name *</label>
            <input
              type="text"
              name="lastName"
              value={formData.lastName}
              onChange={handleChange}
              required
            />
          </div>
        </div>

        <div className="form-group">
          <label>Phone</label>
          <input
            type="tel"
            name="phone"
            value={formData.phone}
            onChange={handleChange}
          />
        </div>

        {!user && (
          <div className="form-group">
            <label>Password *</label>
            <input
              type="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
              minLength={8}
            />
            <small>Min 8 characters</small>
          </div>
        )}

        <div className="form-group">
          <label className="checkbox-label">
            <input
              type="checkbox"
              name="isActive"
              checked={formData.isActive}
              onChange={handleChange}
            />
            Active
          </label>
        </div>

        <div className="form-group">
          <label>Roles</label>
          <div className="roles-checkboxes">
            {roles.map(role => (
              <label key={role.id} className="checkbox-label">
                <input
                  type="checkbox"
                  checked={selectedRoles.includes(role.id)}
                  onChange={() => handleRoleToggle(role.id)}
                />
                {role.name}
              </label>
            ))}
          </div>
        </div>

        <div className="form-actions">
          <button type="submit" disabled={loading} className="btn-submit">
            {loading ? 'Saving...' : user ? 'Update User' : 'Create User'}
          </button>
          <button type="button" onClick={onCancel} className="btn-cancel">
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
};

export default UserForm;
