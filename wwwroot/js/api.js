const API = {
    async get(endpoint, lang = 'ka') {
        try {
            const response = await fetch(`/api/${endpoint}?lang=${lang}`);
            if (!response.ok) throw new Error(`API Error: ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error(`Fetch Error (${endpoint}):`, error);
            return null;
        }
    }
};