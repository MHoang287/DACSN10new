# DACNN12

Phiên bản Spring Boot MVC được chuyển đổi từ dự án ASP.NET Core MVC (DACSN10).

## Công nghệ chính

- Java 21 / Spring Boot 3.2
- Spring MVC + Thymeleaf
- Spring Data JPA với SQL Server
- Maven

## Cấu trúc thư mục

```
DACNN12/
├── src/main/java/com/dacnn12
│   ├── domain        # Các entity JPA chuyển đổi từ model C#
│   ├── dto           # DTO sử dụng cho giao diện
│   ├── repository    # Spring Data repositories
│   ├── service       # Lớp dịch vụ và triển khai
│   └── web           # Controller MVC
└── src/main/resources
    ├── templates     # Giao diện Thymeleaf
    ├── static        # CSS tĩnh
    └── application.properties
```

## Cấu hình cơ sở dữ liệu

Chỉnh `src/main/resources/application.properties` cho thông tin SQL Server:

```
spring.datasource.url=jdbc:sqlserver://<host>:1433;databaseName=DACNN12;encrypt=false
spring.datasource.username=<username>
spring.datasource.password=<password>
```

Sử dụng `spring.jpa.hibernate.ddl-auto=update` để Hibernate tự tạo/tự cập nhật bảng dựa trên entity.

## Chạy ứng dụng

```
mvn spring-boot:run
```

Ứng dụng sẽ chạy tại `http://localhost:8080`.

Trang chủ hiển thị thống kê, danh sách khóa học nổi bật và khóa học mới nhất. Các trang `/courses` cung cấp chức năng xem danh sách, tìm kiếm và xem chi tiết khóa học tương đương với bản ASP.NET.
