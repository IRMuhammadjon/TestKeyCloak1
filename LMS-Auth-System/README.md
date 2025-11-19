# LMS Authentication System with Keycloak

To'liq ishlaydigan LMS (Learning Management System) autentifikatsiya tizimi Keycloak integratsiyasi bilan.

## Arxitektura

Bu loyiha microservices arxitekturasi asosida qurilgan:

- **PostgreSQL** - Asosiy database (LMS ma'lumotlari)
- **Keycloak** - Authentication va Authorization server
- **User Service** - User, Role, Permission CRUD + Keycloak Sync
- **Auth Service** - Token validation va user info
- **Frontend** - React TypeScript UI

## Funksionallik

### ✨ Asosiy Xususiyatlar

1. **Keycloak Integration**
   - JWT token-based authentication
   - Token validation (har bir serviceda)
   - Role-based access control (RBAC)

2. **User Management**
   - User CRUD operations
   - Role assignment/removal
   - Permission management

3. **Keycloak Synchronization**
   - External DB ↔ Keycloak sinxronizatsiya
   - User create/update/delete → Keycloak sync
   - Role assignment → Keycloak sync
   - API orqali avtomatik sinxronizatsiya

4. **Frontend UI**
   - Keycloak login page
   - User profile display
   - Admin panel (admin rolesi uchun)
   - Logout functionality

## Pre-configured Users

Keycloak da 2 ta default user mavjud:

| Username | Password | Roles | Description |
|----------|----------|-------|-------------|
| `admin` | `admin123` | admin, user | Administrator - to'liq access |
| `testuser` | `user123` | user, student | Oddiy user - cheklangan access |

## Qanday Ishlatish

### 1. Prerequisites

Sizning kompyuteringizda quyidagilar o'rnatilgan bo'lishi kerak:
- Docker & Docker Compose
- .NET 8 SDK (lokal development uchun)
- Node.js 18+ (lokal development uchun)

### 2. Loyihani Run Qilish

```bash
# 1. Loyiha papkasiga kiring
cd LMS-Auth-System

# 2. Docker Compose bilan hamma servislarni ishga tushiring
docker-compose up --build

# Bu 5-7 daqiqa vaqt olishi mumkin (birinchi marta)
```

### 3. Servislarni Tekshirish

Hamma servislar ishga tushgandan so'ng:

```bash
# PostgreSQL ready
✅ postgres (http://localhost:5432)

# Keycloak ready
✅ keycloak (http://localhost:8080)
   Admin console: http://localhost:8080/admin
   Username: admin
   Password: admin123

# User Service ready
✅ user-service (http://localhost:5001)
   Swagger: http://localhost:5001/swagger

# Auth Service ready
✅ auth-service (http://localhost:5002)
   Swagger: http://localhost:5002/swagger

# Frontend ready
✅ frontend (http://localhost:3000)
```

### 4. Tizimga Kirish (Login)

1. Brauzerda ochting: **http://localhost:3000**

2. Keycloak login page ochiladi

3. **Admin sifatida kirish:**
   - Username: `admin`
   - Password: `admin123`
   - Login tugmasini bosing

4. Muvaffaqiyatli kirganingizdan keyin:
   - Sizning profil ma'lumotlaringiz ko'rinadi
   - **"Open Admin Panel"** tugmasi ko'rinadi (admin role uchun)
   - User ma'lumotlari: username, email, roles

5. **Admin Panel Ishlatish:**
   - "Open Admin Panel" tugmasini bosing
   - User List ko'rinadi (barcha userlar)
   - **"+ Create New User"** tugmasini bosing
   - Formani to'ldiring:
     - Username: `newuser`
     - Email: `newuser@lms.local`
     - First Name: `New`
     - Last Name: `User`
     - Password: `Password123!`
     - Roles: `user`, `student` (checkbox)
   - **"Create User"** bosing
   - User yaratiladi va Keycloak ga avtomatik sinxronlanadi!
   - User listda yangi user ko'rinadi

6. **User ni Edit qiling:**
   - User listdan "Edit" tugmasini bosing
   - Ma'lumotlarni o'zgartiring
   - Rolelarni o'zgartirish mumkin (add/remove)
   - "Update User" bosing
   - O'zgarishlar Keycloak ga avtomatik sync bo'ladi

7. **Logout qiling:**
   - "Logout" tugmasini bosing
   - Keycloak login pagega qaytasiz

8. **Yangi user sifatida kirish:**
   - Username: `newuser`
   - Password: `Password123!`
   - Login tugmasini bosing
   - User panel ko'rinadi (admin panel yo'q, chunki admin role yo'q)

9. **Oddiy user sifatida kirish:**
   - Logout qiling
   - Username: `testuser`
   - Password: `user123`
   - Login tugmasini bosing
   - Admin panel ko'rinmaydi (chunki admin role yo'q)

## API Testing (Swagger)

### User Service API (http://localhost:5001/swagger)

**Autentifikatsiya kerak!** Token olish:

1. Frontend ga admin sifatida kiring
2. Browser DevTools → Application → Local Storage
3. Token ni copy qiling
4. Swagger UI da "Authorize" tugmasini bosing
5. Token ni kiriting: `Bearer {your-token}`

#### User Management Endpoints:

```
GET    /api/Users              - Barcha userlarni olish
GET    /api/Users/{id}         - Bitta userni olish
POST   /api/Users              - Yangi user yaratish
PUT    /api/Users/{id}         - Userni yangilash
DELETE /api/Users/{id}         - Userni o'chirish
POST   /api/Users/{userId}/roles/{roleId}   - Userga role biriktirish
DELETE /api/Users/{userId}/roles/{roleId}   - Userdan role olish
```

#### Example: Yangi User Yaratish

```json
POST /api/Users
{
  "username": "newuser",
  "email": "newuser@lms.local",
  "firstName": "New",
  "lastName": "User",
  "phone": "+998901234569",
  "password": "NewUser123!"
}
```

Bu user avtomatik ravishda:
1. PostgreSQL database ga qo'shiladi
2. Keycloak ga sinxronlanadi
3. Keycloak ID oladi

### Auth Service API (http://localhost:5002/swagger)

```
GET /api/Auth/validate      - Tokenni validatsiya qilish
GET /api/Auth/user-info     - User ma'lumotlarini olish
GET /api/Auth/check-role/{role}  - Roleni tekshirish
GET /api/Auth/health        - Service health check
```

## Keycloak Sync Mexanizmi

User Service da **KeycloakSyncService** mavjud:

### User Operatsiyalari

```csharp
// User yaratish → Keycloak ga sync
POST /api/Users
→ PostgreSQL ga save
→ Keycloak da user create
→ Keycloak ID ni PostgreSQL ga update
→ User roles ni sync

// User yangilash → Keycloak ga sync
PUT /api/Users/{id}
→ PostgreSQL da update
→ Keycloak da update

// User o'chirish → Keycloak dan o'chirish
DELETE /api/Users/{id}
→ PostgreSQL dan o'chirish
→ Keycloak dan o'chirish
```

### Role Operatsiyalari

```csharp
// Userga role biriktirish
POST /api/Users/{userId}/roles/{roleId}
→ user_roles jadvaliga qo'shish (PostgreSQL)
→ Keycloak da user ga role assign

// Userdan role olish
DELETE /api/Users/{userId}/roles/{roleId}
→ user_roles jadvalidan o'chirish
→ Keycloak dan role remove
```

## Database Schema

```sql
lms.users            - Foydalanuvchilar
lms.roles            - Rollar (admin, user, teacher, student)
lms.permissions      - Permissionlar
lms.user_roles       - User-Role mapping
lms.user_permissions - User-Permission mapping
lms.role_permissions - Role-Permission mapping
lms.sync_history     - Keycloak sync tarixi
```

## Development

### Lokal Run (Docker siz)

```bash
# 1. PostgreSQL va Keycloak ni Docker da ishga tushiring
docker-compose up postgres keycloak

# 2. User Service ni run qiling
cd services/LMS.UserService
dotnet run

# 3. Auth Service ni run qiling (boshqa terminalda)
cd services/LMS.AuthService
dotnet run

# 4. Frontend ni run qiling (boshqa terminalda)
cd frontend/lms-ui
npm start
```

## Environment Variables

### User Service

```env
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=lms_db;Username=lms_user;Password=lms_password
Keycloak__AuthServerUrl=http://keycloak:8080
Keycloak__Realm=lms-realm
Keycloak__ClientId=lms-user-service
Keycloak__ClientSecret=lms-user-service-secret
```

### Auth Service

```env
Keycloak__AuthServerUrl=http://keycloak:8080
Keycloak__Realm=lms-realm
Keycloak__ClientId=lms-auth-service
Keycloak__ClientSecret=lms-auth-service-secret
```

### Frontend

```env
REACT_APP_KEYCLOAK_URL=http://localhost:8080
REACT_APP_KEYCLOAK_REALM=lms-realm
REACT_APP_KEYCLOAK_CLIENT_ID=lms-frontend
```

## Troubleshooting

### Keycloak ishga tushmayapti

```bash
# Keycloak containerini restart qiling
docker-compose restart keycloak

# Loglarni tekshiring
docker-compose logs keycloak
```

### Database connection error

```bash
# PostgreSQL tayyor ekanligini tekshiring
docker-compose logs postgres

# Database ga connect bo'lish
docker exec -it lms-postgres psql -U lms_user -d lms_db
```

### Frontend ga kirish mumkin emas

```bash
# Keycloak realm import bo'lganligini tekshiring
docker-compose logs keycloak | grep "realm-export.json"

# Keycloak admin console ga kiring va realm tekshiring
http://localhost:8080/admin
```

## Qo'shimcha Ma'lumotlar

### Roles

- `admin` - To'liq access (barcha operatsiyalar)
- `user` - Asosiy user access
- `teacher` - O'qituvchi role (kurs yaratish, tahrirlash)
- `student` - Talaba role (kurslarni ko'rish, enroll)

### Permissions

```
users.*          - User management
roles.*          - Role management
permissions.*    - Permission management
courses.*        - Course management
```

### Token Validation

Har bir service o'z ichida token validation qiladi:

```csharp
// Shared library dan foydalanish
services.AddKeycloakAuthentication(configuration);
```

## Security

- JWT tokens (1 soat lifetime)
- Role-based access control
- Password policy (min 8 char, upper, lower, digits)
- HTTPS ready (production uchun)

## License

MIT

## Support

Issues: [GitHub Issues](https://github.com/yourusername/lms-auth-system/issues)

---

**Muallif:** LMS Development Team
**Versiya:** 1.0.0
**Sana:** 2025-11-19
