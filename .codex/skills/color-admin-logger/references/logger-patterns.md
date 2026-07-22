# Logger Advanced Patterns

Patrones avanzados y casos de uso específicos para el LoggerService.

## Table of Contents

- [Complex HTTP Scenarios](#complex-http-scenarios)
- [State Management Logging](#state-management-logging)
- [Performance Monitoring](#performance-monitoring)
- [Error Context Enrichment](#error-context-enrichment)
- [Conditional Logging](#conditional-logging)
- [Integration with RxJS](#integration-with-rxjs)

---

## Complex HTTP Scenarios

### Upload con Progress

```typescript
uploadFile(file: File) {
  this.logger.debug('Starting file upload:', file.name);
  
  return this.http.post('/api/upload', formData, {
    reportProgress: true,
    observe: 'events'
  }).pipe(
    tap(event => {
      if (event.type === HttpEventType.UploadProgress) {
        const progress = Math.round(100 * event.loaded / event.total!);
        this.logger.debug(`Upload progress: ${progress}%`);
      }
      if (event.type === HttpEventType.Response) {
        this.logger.success('File uploaded successfully');
      }
    }),
    catchError(error => {
      this.logger.error('Upload failed:', error);
      return throwError(() => error);
    })
  );
}
```

### Retry Logic con Logging

```typescript
fetchDataWithRetry() {
  this.logger.debug('Fetching data with retry logic');
  
  return this.http.get('/api/data').pipe(
    retry({
      count: 3,
      delay: (error, retryCount) => {
        this.logger.warn(`Retry attempt ${retryCount} after error:`, error);
        return timer(1000 * retryCount);
      }
    }),
    tap(response => {
      this.logger.success('Data fetched successfully');
    }),
    catchError(error => {
      this.logger.error('All retry attempts failed:', error);
      return throwError(() => error);
    })
  );
}
```

### Batch Requests

```typescript
async processBatch(items: Item[]) {
  this.logger.group('Processing batch');
  this.logger.info(`Total items: ${items.length}`);
  
  const results = [];
  let successCount = 0;
  let errorCount = 0;
  
  for (const [index, item] of items.entries()) {
    try {
      this.logger.debug(`Processing item ${index + 1}/${items.length}`);
      const result = await this.processItem(item);
      results.push(result);
      successCount++;
    } catch (error) {
      this.logger.error(`Failed to process item ${index + 1}:`, error);
      errorCount++;
    }
  }
  
  this.logger.info(`Batch complete: ${successCount} success, ${errorCount} errors`);
  this.logger.groupEnd();
  
  return results;
}
```

---

## State Management Logging

### Signal State Changes

```typescript
export class UserStateService {
  private readonly logger = inject(LoggerService).getLogger('UserStateService');
  
  private readonly userSignal = signal<User | null>(null);
  
  // Computed state con logging
  readonly isAuthenticated = computed(() => {
    const user = this.userSignal();
    const isAuth = user !== null;
    this.logger.debug('Auth state computed:', isAuth);
    return isAuth;
  });
  
  setUser(user: User) {
    this.logger.debug('Setting user:', user.email);
    this.userSignal.set(user);
  }
  
  clearUser() {
    this.logger.info('Clearing user session');
    this.userSignal.set(null);
  }
}
```

### Effect Logging

```typescript
export class CartComponent {
  private readonly logger = inject(LoggerService).getLogger('CartComponent');
  
  cartItems = signal<CartItem[]>([]);
  
  constructor() {
    // Log cambios en el carrito
    effect(() => {
      const items = this.cartItems();
      const total = items.reduce((sum, item) => sum + item.price, 0);
      
      this.logger.debug('Cart updated:', {
        itemCount: items.length,
        total: total
      });
    });
  }
}
```

---

## Performance Monitoring

### Timing Operations

```typescript
async loadLargeDataset() {
  const startTime = performance.now();
  this.logger.debug('Starting large dataset load');
  
  try {
    const data = await this.fetchData();
    const endTime = performance.now();
    const duration = (endTime - startTime).toFixed(2);
    
    this.logger.success(`Data loaded in ${duration}ms`);
    this.logger.debug(`Records loaded: ${data.length}`);
    
    return data;
  } catch (error) {
    const endTime = performance.now();
    const duration = (endTime - startTime).toFixed(2);
    
    this.logger.error(`Failed after ${duration}ms:`, error);
    throw error;
  }
}
```

### Component Render Tracking

```typescript
export class PerformanceTrackedComponent {
  private readonly logger = inject(LoggerService).getLogger('PerformanceComponent');
  private renderCount = 0;
  
  ngOnInit() {
    this.logger.debug('Component initialized');
  }
  
  ngAfterViewInit() {
    this.renderCount++;
    this.logger.debug(`Rendered ${this.renderCount} times`);
  }
  
  ngOnDestroy() {
    this.logger.debug(`Component destroyed after ${this.renderCount} renders`);
  }
}
```

---

## Error Context Enrichment

### Structured Error Logging

```typescript
handleApiError(error: any, context: string) {
  const errorInfo = {
    context,
    timestamp: new Date().toISOString(),
    status: error.status,
    message: error.message,
    url: error.url,
    user: this.authService.getCurrentUserEmail()
  };
  
  this.logger.group('API Error Details');
  this.logger.error('Error occurred in:', context);
  this.logger.error('Status code:', error.status);
  this.logger.error('Message:', error.message);
  this.logger.error('URL:', error.url);
  this.logger.error('Current user:', errorInfo.user);
  this.logger.groupEnd();
  
  // Enviar a servicio de monitoreo si existe
  this.monitoringService?.logError(errorInfo);
}
```

### Validation Errors

```typescript
validateForm(form: FormGroup) {
  if (!form.valid) {
    this.logger.warn('Form validation failed');
    
    const errors = [];
    Object.keys(form.controls).forEach(key => {
      const control = form.get(key);
      if (control?.errors) {
        errors.push({ field: key, errors: control.errors });
      }
    });
    
    this.logger.table(errors);
    return false;
  }
  
  this.logger.debug('Form validation passed');
  return true;
}
```

---

## Conditional Logging

### Feature Flag Logging

```typescript
async executeFeature(featureName: string) {
  const isEnabled = await this.featureFlags.isEnabled(featureName);
  
  if (isEnabled) {
    this.logger.info(`Feature '${featureName}' is enabled`);
    // Execute feature
  } else {
    this.logger.warn(`Feature '${featureName}' is disabled`);
    // Fallback
  }
}
```

### User Role Based Logging

```typescript
performAdminAction(action: string) {
  const user = this.authService.getCurrentUser();
  
  this.logger.group('Admin Action');
  this.logger.info('Action:', action);
  this.logger.debug('User:', user.email);
  this.logger.debug('Role:', user.role);
  this.logger.debug('Permissions:', user.permissions);
  this.logger.groupEnd();
  
  if (user.role === 'admin') {
    this.logger.success('Admin action authorized');
    // Proceed
  } else {
    this.logger.error('Unauthorized admin action attempt');
    throw new UnauthorizedError();
  }
}
```

---

## Integration with RxJS

### Observable Pipeline Logging

```typescript
loadUserData(userId: string) {
  return this.http.get<User>(`/api/users/${userId}`).pipe(
    // Log inicio
    tap(() => this.logger.debug('Fetching user:', userId)),
    
    // Transformación con log
    map(user => {
      this.logger.debug('User data received:', user.email);
      return this.transformUser(user);
    }),
    
    // Log transformación
    tap(transformed => {
      this.logger.debug('User transformed');
    }),
    
    // Manejo de errores con log
    catchError(error => {
      this.logger.error('Failed to load user:', error);
      return of(null);
    }),
    
    // Log final
    finalize(() => {
      this.logger.debug('User load completed');
    })
  );
}
```

### Subject/BehaviorSubject Logging

```typescript
export class NotificationService {
  private readonly logger = inject(LoggerService).getLogger('NotificationService');
  
  private notifications$ = new BehaviorSubject<Notification[]>([]);
  
  addNotification(notification: Notification) {
    const current = this.notifications$.value;
    const updated = [...current, notification];
    
    this.logger.debug('Adding notification:', notification.message);
    this.logger.debug('Total notifications:', updated.length);
    
    this.notifications$.next(updated);
  }
  
  clearNotifications() {
    this.logger.info('Clearing all notifications');
    this.notifications$.next([]);
  }
}
```

### switchMap with Logging

```typescript
searchUsers(searchTerm: string) {
  return of(searchTerm).pipe(
    tap(term => this.logger.debug('Search term:', term)),
    
    debounceTime(300),
    tap(() => this.logger.debug('Debounce complete')),
    
    distinctUntilChanged(),
    tap(() => this.logger.debug('Search term changed')),
    
    switchMap(term => {
      if (!term) {
        this.logger.debug('Empty search term, returning empty');
        return of([]);
      }
      
      this.logger.debug('Executing search for:', term);
      return this.http.get<User[]>(`/api/users/search?q=${term}`).pipe(
        tap(results => {
          this.logger.success(`Found ${results.length} users`);
        }),
        catchError(error => {
          this.logger.error('Search failed:', error);
          return of([]);
        })
      );
    })
  );
}
```

---

## WebSocket Logging

```typescript
export class WebSocketService {
  private readonly logger = inject(LoggerService).getLogger('WebSocketService');
  private socket?: WebSocket;
  
  connect(url: string) {
    this.logger.info('Connecting to WebSocket:', url);
    
    this.socket = new WebSocket(url);
    
    this.socket.onopen = () => {
      this.logger.success('WebSocket connected');
    };
    
    this.socket.onmessage = (event) => {
      this.logger.debug('Message received:', event.data);
    };
    
    this.socket.onerror = (error) => {
      this.logger.error('WebSocket error:', error);
    };
    
    this.socket.onclose = () => {
      this.logger.warn('WebSocket closed');
    };
  }
  
  send(message: any) {
    if (this.socket?.readyState === WebSocket.OPEN) {
      this.logger.debug('Sending message:', message);
      this.socket.send(JSON.stringify(message));
    } else {
      this.logger.error('Cannot send, WebSocket not open');
    }
  }
}
```

---

## IndexedDB/Storage Logging

```typescript
export class StorageService {
  private readonly logger = inject(LoggerService).getLogger('StorageService');
  
  async saveToStorage(key: string, value: any) {
    try {
      this.logger.debug('Saving to storage:', key);
      await this.storage.set(key, value);
      this.logger.success('Saved successfully');
    } catch (error) {
      this.logger.error('Failed to save:', error);
      throw error;
    }
  }
  
  async loadFromStorage(key: string) {
    try {
      this.logger.debug('Loading from storage:', key);
      const value = await this.storage.get(key);
      
      if (value) {
        this.logger.success('Loaded successfully');
      } else {
        this.logger.warn('No data found for key:', key);
      }
      
      return value;
    } catch (error) {
      this.logger.error('Failed to load:', error);
      throw error;
    }
  }
}
```

---

## Testing with Logger

```typescript
describe('UserService', () => {
  let service: UserService;
  let logger: LoggerService;
  
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        UserService,
        LoggerService
      ]
    });
    
    service = TestBed.inject(UserService);
    logger = TestBed.inject(LoggerService);
    
    // Spy on logger methods
    spyOn(logger, 'debug');
    spyOn(logger, 'error');
  });
  
  it('should log debug when loading user', async () => {
    await service.loadUser('123');
    
    expect(logger.debug).toHaveBeenCalledWith(
      jasmine.stringContaining('Loading user')
    );
  });
  
  it('should log error on failure', async () => {
    // Simulate error
    await service.loadUser('invalid').catch(() => {});
    
    expect(logger.error).toHaveBeenCalled();
  });
});
```

---

## Best Practices Summary

1. **Always use context**: `getLogger('ServiceName')`
2. **Debug for development**: Internal details go to `debug()`
3. **Info for flow**: Important events use `info()` sparingly
4. **Group related logs**: Use `group()` and `groupEnd()`
5. **Never log secrets**: Passwords, tokens, API keys
6. **Performance awareness**: Heavy logging only in debug
7. **Structured data**: Use `table()` for arrays
8. **Error context**: Include relevant context in errors
9. **HTTP logging**: Use `http()` and `httpResponse()`
10. **Test your logs**: Verify logs in both dev and prod builds
