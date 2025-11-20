import React, { useEffect, useState } from 'react';
import { userApi } from '../services/api';
import { User } from '../types';
import UserPermissions from './UserPermissions';
import './UserList.css';

interface UserListProps {
  onEdit: (user: User) => void;
  onDelete: (userId: string) => void;
  refresh: number;
}

const UserList: React.FC<UserListProps> = ({ onEdit, onDelete, refresh }) => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  useEffect(() => {
    loadUsers();
  }, [refresh]);

  const loadUsers = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await userApi.getAll();
      setUsers(data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load users');
      console.error('Error loading users:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="loading">Loading users...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <div className="user-list">
      <h2>Users ({users.length})</h2>
      <table>
        <thead>
          <tr>
            <th>Username</th>
            <th>Name</th>
            <th>Email</th>
            <th>Roles</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id}>
              <td><strong>{user.username}</strong></td>
              <td>{user.firstName} {user.lastName}</td>
              <td>{user.email}</td>
              <td>
                {user.userRoles?.map(ur => ur.role.name).join(', ') || 'No roles'}
              </td>
              <td>
                <span className={`status ${user.isActive ? 'active' : 'inactive'}`}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </span>
              </td>
              <td>
                <button onClick={() => setSelectedUser(user)} className="btn-permissions">
                  Permissions
                </button>
                <button onClick={() => onEdit(user)} className="btn-edit">
                  Edit
                </button>
                <button
                  onClick={() => {
                    if (window.confirm(`Delete user ${user.username}?`)) {
                      onDelete(user.id);
                    }
                  }}
                  className="btn-delete"
                >
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {users.length === 0 && (
        <p className="no-data">No users found</p>
      )}

      {selectedUser && (
        <UserPermissions
          userId={selectedUser.id}
          username={selectedUser.username}
          onClose={() => setSelectedUser(null)}
        />
      )}
    </div>
  );
};

export default UserList;
