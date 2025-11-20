import React, { useState, useEffect } from 'react';
import { permissionApi } from '../services/api';
import { Permission, UserPermissionsResponse } from '../types';
import './UserPermissions.css';

interface UserPermissionsProps {
  userId: string;
  username: string;
  onClose: () => void;
}

const UserPermissions: React.FC<UserPermissionsProps> = ({ userId, username, onClose }) => {
  const [userPermissions, setUserPermissions] = useState<UserPermissionsResponse | null>(null);
  const [availablePermissions, setAvailablePermissions] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, [userId]);

  const loadData = async () => {
    try {
      setLoading(true);
      const [userPerms, allPerms] = await Promise.all([
        permissionApi.getUserPermissions(userId),
        permissionApi.getAll(),
      ]);
      setUserPermissions(userPerms);
      setAvailablePermissions(allPerms);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load permissions');
      console.error('Error loading permissions:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAssignPermission = async (permissionId: string) => {
    try {
      await permissionApi.assignToUser(permissionId, userId);
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to assign permission');
    }
  };

  const handleRemovePermission = async (permissionId: string) => {
    try {
      await permissionApi.removeFromUser(permissionId, userId);
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to remove permission');
    }
  };

  const isAssigned = (permissionId: string) => {
    return userPermissions?.permissions.some(p => p.id === permissionId) || false;
  };

  const getPermissionSource = (permissionId: string) => {
    const perm = userPermissions?.permissions.find(p => p.id === permissionId);
    return perm?.source || '';
  };

  if (loading) {
    return (
      <div className="modal-overlay">
        <div className="modal-content">
          <div className="loading">Loading permissions...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content user-permissions-modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Permissions for {username}</h2>
          <button onClick={onClose} className="btn-close">&times;</button>
        </div>

        {error && <div className="error">{error}</div>}

        <div className="modal-body">
          <div className="permissions-summary">
            <h3>Current Permissions ({userPermissions?.permissions.length || 0})</h3>
            {userPermissions?.permissions.length === 0 ? (
              <p className="no-permissions">No permissions assigned</p>
            ) : (
              <div className="permissions-list">
                {userPermissions?.permissions.map(perm => (
                  <div key={perm.id} className="permission-item assigned">
                    <div className="permission-info">
                      <strong>{perm.name}</strong>
                      <span className="permission-details">
                        {perm.resource}:{perm.action}
                      </span>
                      <span className={`permission-source ${perm.source === 'direct' ? 'direct' : 'inherited'}`}>
                        {perm.source === 'direct' ? 'Direct' : perm.source}
                      </span>
                    </div>
                    {perm.source === 'direct' && (
                      <button
                        onClick={() => handleRemovePermission(perm.id)}
                        className="btn-remove"
                      >
                        Remove
                      </button>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="permissions-available">
            <h3>Available Permissions</h3>
            <div className="permissions-list">
              {availablePermissions.map(perm => {
                const assigned = isAssigned(perm.id);
                const source = getPermissionSource(perm.id);

                return (
                  <div key={perm.id} className={`permission-item ${assigned ? 'assigned' : ''}`}>
                    <div className="permission-info">
                      <strong>{perm.name}</strong>
                      <span className="permission-details">
                        {perm.resource}:{perm.action}
                      </span>
                      {perm.description && (
                        <span className="permission-description">{perm.description}</span>
                      )}
                    </div>
                    {!assigned ? (
                      <button
                        onClick={() => handleAssignPermission(perm.id)}
                        className="btn-assign"
                      >
                        Assign
                      </button>
                    ) : (
                      <span className="assigned-badge">
                        {source === 'direct' ? 'Assigned' : 'From role'}
                      </span>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        <div className="modal-footer">
          <button onClick={onClose} className="btn-close-modal">
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default UserPermissions;
