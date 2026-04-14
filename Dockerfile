FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# העתקת כל הקבצים מהמאגר לתוך מיכל הבנייה
COPY . .

# פקודה גמישה שמוצאת את קובץ הפרויקט בתוך תיקיית TodoApi ומבצעת Restore
RUN dotnet restore "TodoApi/TodoApi.csproj"

# בנייה ופרסום של הפרויקט
RUN dotnet publish "TodoApi/TodoApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# העתקת הקבצים המוכנים לריצה
COPY --from=build /app/publish .

# הגדרות סביבה ל-Render
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# הרצה של ה-API
ENTRYPOINT ["dotnet", "TodoApi.dll"]
