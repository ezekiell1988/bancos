# Color Admin Logger Skill

Sistema de logging centralizado para Angular 20+ que controla automáticamente los logs según el environment (desarrollo/producción).

## When to Use

Use this skill when:
- Implementing logging in components or services
- Converting `console.log/warn/error` to structured logging
- Setting up logging for new features
- Debugging and need consistent log formatting
- Handling HTTP requests/responses logging
- Need different log behavior in dev vs production

## Core Principles

✅ **Control automático** - Logs disabled in production automatically  
✅ **Contexto** - Every log knows which component/service created it  
✅ **Niveles** - debug, info, warn, error, success  
✅ **Zero overhead** - No performance impact in production  
✅ **Type-safe** - Full TypeScript support  

## Implementation

### 1. Service Injection

Always inject LoggerService with context identifier:

```typescript
import { Component, inject } from '@angular/core';
import { LoggerService } from '../../service';

@Component({
  selector: 'app-example',
  templateUrl: './example.html'
})
export class ExampleComponent {
  // ✅ Inject with context name (component/service name)
  private readonly logger = inject(LoggerService).getLogger('ExampleComponent');
  
  ngOnInit() {
    this.logger.info('Component initialized');
  }
}
```

### 2. Log Levels

Use appropriate log levels based on purpose:

```typescript
// 🔍 DEBUG - Development only, hidden in production
this.logger.debug('Variable value:', data);
this.logger.debug('Token received:', token.substring(0, 20));

// ℹ️ INFO - Important flow events (use sparingly in prod)
this.logger.info('Component initialized');
this.logger.info('User navigated to:', route);

// ⚠️ WARN - Unexpected but non-critical issues
this.logger.warn('Token expiring in 5 minutes');
this.logger.warn('API returned 404');

// ❌ ERROR - Errors requiring attention
this.logger.error('Failed to load data:', error);
this.logger.error('Authentication failed');

// ✅ SUCCESS - Important operations completed
this.logger.success('Login successful');
this.logger.success('Data saved successfully');
```

### 3. HTTP Logging Pattern

For HTTP requests, use specialized HTTP logging:

```typescript
async loadData() {
  const url = '/api/users';
  const body = { filter: 'active' };
  
  this.logger.http('GET', url, body);
  
  try {
    const response = await this.http.get(url).toPromise();
    this.logger.httpResponse(200, url, response);
    return response;
  } catch (error) {
    this.logger.error('HTTP request failed:', error);
    throw error;
  }
}
```

### 4. Grouped Logs

For related information, use grouping:

```typescript
processOrder(order: Order) {
  this.logger.group('Processing Order');
  this.logger.debug('Order ID:', order.id);
  this.logger.debug('Items:', order.items.length);
  this.logger.debug('Total:', order.total);
  this.logger.debug('Customer:', order.customer);
  this.logger.groupEnd();
}
```

### 5. Table Logs

For arrays of objects, use table format:

```typescript
displayUsers(users: User[]) {
  this.logger.table(users);
  // Renders as formatted table in console
}
```

## Environment Configuration

### Development (`environment.ts`)

```typescript
export const environment = {
  production: false,
  logging: {
    enabled: true,        // ✅ All logs enabled
    showTimestamp: true,
    showContext: true,
    useColors: true
  }
};
```

### Production (`environment.prod.ts`)

```typescript
export const environment = {
  production: true,
  logging: {
    enabled: false,       // ❌ Only critical errors
    showTimestamp: false,
    showContext: false,
    useColors: false
  }
};
```

## Migration from console.*

### Quick Replace Patterns

| Before | After |
|--------|-------|
| `console.log('Info')` | `this.logger.info('Info')` |
| `console.debug('Debug')` | `this.logger.debug('Debug')` |
| `console.warn('Warning')` | `this.logger.warn('Warning')` |
| `console.error('Error')` | `this.logger.error('Error')` |
| `console.log('✅ Success')` | `this.logger.success('Success')` |

### Migration Steps

1. **Add logger to class:**
```typescript
private readonly logger = inject(LoggerService).getLogger('ClassName');
```

2. **Find all console.*** - Use VS Code: `Ctrl/Cmd + Shift + F`
   - Search: `console\.(log|warn|error|info|debug)`

3. **Replace with appropriate level:**
   - Development details → `logger.debug()`
   - Flow information → `logger.info()`
   - Warnings → `logger.warn()`
   - Errors → `logger.error()`
   - Success messages → `logger.success()`

## Best Practices

### ✅ DO

```typescript
// 1. Use correct context
private readonly logger = inject(LoggerService).getLogger('AuthService');

// 2. Use appropriate levels
this.logger.debug('Internal state:', data);  // Dev only
this.logger.error('Critical error:', error); // Prod too

// 3. Descriptive messages
this.logger.info('User authenticated:', user.email);

// 4. Group related logs
this.logger.group('Processing payment');
this.logger.debug('Amount:', payment.amount);
this.logger.debug('Method:', payment.method);
this.logger.groupEnd();
```

### ❌ DON'T

```typescript
// 1. Never use console directly
console.log('Something');  // ❌

// 2. Don't use INFO for debugging
this.logger.info('Variable x =', x);  // ❌ Use debug()

// 3. Never log sensitive data
this.logger.debug('Password:', password);  // ❌ Security risk!

// 4. Don't log everything in production
this.logger.info('Button clicked');  // ❌ Too noisy
```

## Common Patterns

### Component Lifecycle

```typescript
export class ExampleComponent {
  private readonly logger = inject(LoggerService).getLogger('ExampleComponent');

  ngOnInit() {
    this.logger.info('Component initialized');
  }

  ngOnDestroy() {
    this.logger.info('Component destroyed');
  }
}
```

### Service with HTTP

```typescript
@Injectable({ providedIn: 'root' })
export class DataService {
  private readonly logger = inject(LoggerService).getLogger('DataService');
  private readonly http = inject(HttpClient);

  loadData() {
    this.logger.debug('Loading data...');
    
    return this.http.get('/api/data').pipe(
      tap(response => {
        this.logger.success('Data loaded successfully');
        this.logger.debug('Response:', response);
      }),
      catchError(error => {
        this.logger.error('Failed to load data:', error);
        return throwError(() => error);
      })
    );
  }
}
```

### Authentication Flow

```typescript
async login(credentials: Credentials) {
  this.logger.debug('Login attempt for:', credentials.email);
  
  try {
    const response = await this.authService.login(credentials);
    this.logger.success('Login successful');
    this.logger.debug('Token received:', response.token.substring(0, 20) + '...');
    return response;
  } catch (error) {
    this.logger.error('Login failed:', error);
    throw error;
  }
}
```

### Error Handling

```typescript
async performAction() {
  try {
    this.logger.debug('Starting action...');
    const result = await this.someService.doSomething();
    this.logger.success('Action completed');
    return result;
  } catch (error) {
    this.logger.error('Action failed:', error);
    
    if (error instanceof ValidationError) {
      this.logger.warn('Validation errors:', error.details);
    }
    
    throw error;
  }
}
```

## Console Output Examples

### Development (with colors and context)

```
[10:30:45.123] [LoginComponent] 🔍 Login attempt for: user@example.com
[10:30:45.456] [AuthService] 📤 POST /api/login
[10:30:45.789] [AuthService] 📥 200 /api/login
[10:30:45.890] [LoginComponent] ✅ Login successful
[10:30:45.891] [LoginComponent] 🔍 Token received: eyJhbGc...
[10:30:45.950] [LoginComponent] ℹ️ Navigating to: /home
```

### Production (errors only)

```
[AuthService] ❌ Login failed: Unauthorized
```

## Integration Checklist

When implementing logger in a new component/service:

- [ ] Import `LoggerService` from `'../../service'`
- [ ] Inject with `inject(LoggerService).getLogger('ContextName')`
- [ ] Replace all `console.*` calls
- [ ] Use `debug()` for development-only info
- [ ] Use `error()` for errors
- [ ] Use `success()` for important completions
- [ ] Use `info()` sparingly for production flow
- [ ] Never log sensitive data (passwords, tokens, PII)
- [ ] Test in development (logs visible)
- [ ] Test production build (only errors visible)

## References

See `references/logger-patterns.md` for more examples and advanced patterns.

## File Locations

- Service: `src/app/service/logger.service.ts`
- Export: `src/app/service/index.ts`
- Environment: `src/environments/environment.ts`
- Environment Prod: `src/environments/environment.prod.ts`
- Documentation: `docs/ionic-angular/services/logger.md`
