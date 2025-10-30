// JWT Testing and Debugging Script
// Add this to your layout or specific pages for testing

(function () {
    'use strict';

    // Test JWT token functionality
    window.jwtTest = {
        // Check current auth status
        checkStatus: async function () {
            try {
                const response = await fetch('/Account/AuthStatus', {
                    method: 'GET',
                    credentials: 'include'
                });

                const data = await response.json();
                console.log('=== Auth Status ===');
                console.log('IsAuthenticated:', data.isAuthenticated);
                console.log('UserName:', data.userName);
                console.log('Email:', data.email);
                console.log('Roles:', data.roles);
                console.log('Has Token:', data.hasToken);
                console.log('Token Expiry:', data.tokenExpiry);
                console.log('Session Exists:', data.sessionExists);

                return data;
            } catch (error) {
                console.error('Error checking status:', error);
            }
        },

        // Test API call with JWT
        testApiCall: async function (url) {
            try {
                console.log(`Testing API call to: ${url}`);

                const response = await fetch(url, {
                    method: 'GET',
                    credentials: 'include',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });

                console.log('Response status:', response.status);

                if (response.ok) {
                    const data = await response.json();
                    console.log('Response data:', data);
                    return data;
                } else {
                    console.error('API call failed:', response.statusText);
                    const errorText = await response.text();
                    console.error('Error details:', errorText);
                }
            } catch (error) {
                console.error('Error in API call:', error);
            }
        },

        // Manually refresh token
        refreshToken: async function () {
            try {
                console.log('Attempting to refresh token...');

                const response = await fetch('/Account/KeepAlive', {
                    method: 'POST',
                    credentials: 'include',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });

                const data = await response.json();
                console.log('Token refresh result:', data);

                if (data.success) {
                    console.log('✅ Token refreshed successfully');
                } else {
                    console.error('❌ Token refresh failed:', data.message);
                }

                return data;
            } catch (error) {
                console.error('Error refreshing token:', error);
            }
        },

        // Decode JWT token (client-side only for debugging)
        decodeToken: function (token) {
            try {
                const base64Url = token.split('.')[1];
                const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
                const jsonPayload = decodeURIComponent(atob(base64).split('').map(function (c) {
                    return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
                }).join(''));

                const decoded = JSON.parse(jsonPayload);

                console.log('=== Decoded JWT ===');
                console.log('Subject (NameIdentifier):', decoded.nameid || decoded.sub);
                console.log('Email:', decoded.email);
                console.log('Name:', decoded.name || decoded.unique_name);
                console.log('Roles:', decoded.role);
                console.log('Issued At:', new Date(decoded.iat * 1000));
                console.log('Expires At:', new Date(decoded.exp * 1000));
                console.log('Time Until Expiry:', Math.round((decoded.exp * 1000 - Date.now()) / 1000 / 60), 'minutes');

                return decoded;
            } catch (error) {
                console.error('Error decoding token:', error);
                return null;
            }
        },

        // Get token expiry time
        getTokenExpiry: function () {
            const expiryStr = sessionStorage.getItem('TokenExpiry');
            if (expiryStr) {
                const expiry = new Date(expiryStr);
                const now = new Date();
                const minutesLeft = Math.round((expiry - now) / 1000 / 60);

                console.log('Token expires at:', expiry);
                console.log('Time left:', minutesLeft, 'minutes');

                return {
                    expiry: expiry,
                    minutesLeft: minutesLeft,
                    isExpired: minutesLeft <= 0
                };
            } else {
                console.warn('No token expiry found in session');
                return null;
            }
        },

        // Start automatic token refresh
        startAutoRefresh: function (intervalMinutes = 5) {
            console.log(`Starting auto-refresh every ${intervalMinutes} minutes`);

            setInterval(async () => {
                console.log('Auto-refresh triggered');
                await this.refreshToken();
            }, intervalMinutes * 60 * 1000);
        },

        // Test all endpoints
        testAll: async function () {
            console.log('=== Starting comprehensive JWT test ===');

            // 1. Check status
            console.log('\n1. Checking auth status...');
            await this.checkStatus();

            // 2. Test token expiry
            console.log('\n2. Checking token expiry...');
            this.getTokenExpiry();

            // 3. Test API endpoints
            console.log('\n3. Testing API endpoints...');

            // Test books endpoint
            console.log('\n3a. Testing Books API...');
            await this.testApiCall('https://localhost:5003/api/books/getall');

            // Test users endpoint (admin only)
            console.log('\n3b. Testing Admin Users API...');
            await this.testApiCall('https://localhost:5003/api/users/testauth');

            console.log('\n=== JWT test completed ===');
        }
    };

    // Auto-start refresh on authenticated pages
    window.addEventListener('DOMContentLoaded', function () {
        // Check if user is authenticated
        fetch('/Account/AuthStatus')
            .then(response => response.json())
            .then(data => {
                if (data.isAuthenticated && data.hasToken) {
                    console.log('✅ JWT authentication active');

                    // Start auto-refresh every 5 minutes
                    window.jwtTest.startAutoRefresh(5);

                    // Log token info
                    window.jwtTest.getTokenExpiry();
                } else {
                    console.log('ℹ️ User not authenticated or no JWT token');
                }
            })
            .catch(error => {
                console.error('Error checking auth status:', error);
            });
    });

    console.log('✅ JWT Test utilities loaded. Use window.jwtTest for testing.');
    console.log('Available commands:');
    console.log('  - window.jwtTest.checkStatus()');
    console.log('  - window.jwtTest.testApiCall(url)');
    console.log('  - window.jwtTest.refreshToken()');
    console.log('  - window.jwtTest.getTokenExpiry()');
    console.log('  - window.jwtTest.testAll()');
})();