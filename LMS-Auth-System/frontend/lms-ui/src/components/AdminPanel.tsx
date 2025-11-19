import React, { useState } from 'react';
import { userApi } from '../services/api';
import { User } from '../types';
import UserList from './UserList';
import UserForm from './UserForm';
import './AdminPanel.css';

const AdminPanel: React.FC = () => {
  const [showForm, setShowForm] = useState(false);
  const [editingUser, setEditingUser] = useState<User | undefined>(undefined);
  const [refreshKey, setRefreshKey] = useState(0);

  const handleCreateNew = () => {
    setEditingUser(undefined);
    setShowForm(true);
  };

  const handleEdit = (user: User) => {
    setEditingUser(user);
    setShowForm(true);
  };

  const handleDelete = async (userId: string) => {
    try {
      await userApi.delete(userId);
      setRefreshKey(prev => prev + 1);
      alert('User deleted successfully');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to delete user');
    }
  };

  const handleFormSuccess = () => {
    setShowForm(false);
    setEditingUser(undefined);
    setRefreshKey(prev => prev + 1);
    alert(editingUser ? 'User updated successfully' : 'User created successfully');
  };

  const handleFormCancel = () => {
    setShowForm(false);
    setEditingUser(undefined);
  };

  return (
    <div className="admin-panel">
      <div className="admin-header">
        <h1>Admin Panel - User Management</h1>
        {!showForm && (
          <button onClick={handleCreateNew} className="btn-create">
            + Create New User
          </button>
        )}
      </div>

      {showForm ? (
        <UserForm
          user={editingUser}
          onSuccess={handleFormSuccess}
          onCancel={handleFormCancel}
        />
      ) : (
        <UserList
          onEdit={handleEdit}
          onDelete={handleDelete}
          refresh={refreshKey}
        />
      )}
    </div>
  );
};

export default AdminPanel;
