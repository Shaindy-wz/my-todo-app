# שלב ראשון: בנייה
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# העתקת קובץ הפרויקט וביצוע Restore
COPY ["*.csproj", "./"]
RUN dotnet restore
# העתקת שאר הקבצים ובנייה
COPY . .
RUN dotnet publish -c Release -o /app/publish

# שלב שני: הרצה
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TodoApi.dll"]